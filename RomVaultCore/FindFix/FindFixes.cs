using System;
using System.Collections.Generic;
using System.Diagnostics;
using FileHeaderReader;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;

namespace RomVaultCore.FindFix {
	public static class FindFixes {
		private static ThreadWorker _thWrk;
		private static int _progressCounter;

		public static void ScanFiles(ThreadWorker thWrk) {
			try {
				_thWrk = thWrk;
				if (_thWrk == null) {
					return;
				}

				_progressCounter = 0;

				var sw = new Stopwatch();
				sw.Reset();
				sw.Start();

				_thWrk.Report(new bgwSetRange(12));

				_thWrk.Report(new bgwText("Clearing DB Status"));
				_thWrk.Report(_progressCounter++);
				RepairStatus.ReportStatusReset(DB.DirRoot);
				ResetFileGroups(DB.DirRoot);

				_thWrk.Report(new bgwText("Getting Selected Files"));
				_thWrk.Report(_progressCounter++);

				Debug.WriteLine("Start " + sw.ElapsedMilliseconds);
				var filesGot = new List<RvFile>();
				var filesMissing = new List<RvFile>();
				GetSelectedFiles(DB.DirRoot, true, filesGot, filesMissing);
				Debug.WriteLine("GetSelected " + sw.ElapsedMilliseconds);

				_thWrk.Report(new bgwText("Sorting on CRC"));
				_thWrk.Report(_progressCounter++);
				var filesGotSortedCRC = FindFixesSort.SortCRC(filesGot);
				Debug.WriteLine("SortCRC " + sw.ElapsedMilliseconds);

				// take the fileGot list and fileGroups list
				// this groups all the got files using there CRC

				_thWrk.Report(new bgwText("Index creation on got CRC"));
				_thWrk.Report(_progressCounter++);
				MergeGotFiles(filesGotSortedCRC, out var fileGroupsCRCSorted);

				Debug.WriteLine("Merge " + sw.ElapsedMilliseconds);

				_thWrk.Report(new bgwText("Index creation on got SHA1"));
				_thWrk.Report(_progressCounter++);
				FindFixesSort.SortFamily(fileGroupsCRCSorted, FindSHA1, FamilySortSHA1, out var fileGroupsSHA1Sorted);
				_thWrk.Report(new bgwText("Index creation on got MD5"));
				_thWrk.Report(_progressCounter++);
				FindFixesSort.SortFamily(fileGroupsCRCSorted, FindMD5, FamilySortMD5, out var fileGroupsMD5Sorted);

				// next make another sorted list of got files on the AltCRC
				// these are the same FileGroup classes as in the fileGroupsCRCSorted List, just sorted by AltCRC
				// if the files does not have an altCRC then it is not added to this list.
				_thWrk.Report(new bgwText("Index creation on got AltCRC"));
				_thWrk.Report(_progressCounter++);
				FindFixesSort.SortFamily(fileGroupsCRCSorted, FindAltCRC, FamilySortAltCRC, out var fileGroupsAltCRCSorted);
				_thWrk.Report(new bgwText("Index creation on got AltSHA1"));
				_thWrk.Report(_progressCounter++);
				FindFixesSort.SortFamily(fileGroupsCRCSorted, FindAltSHA1, FamilySortAltSHA1, out var fileGroupsAltSHA1Sorted);
				_thWrk.Report(new bgwText("Index creation on got AltMD5"));
				_thWrk.Report(_progressCounter++);
				FindFixesSort.SortFamily(fileGroupsCRCSorted, FindAltMD5, FamilySortAltMD5, out var fileGroupsAltMD5Sorted);

				_thWrk.Report(new bgwText("Merging in missing file list"));
				_thWrk.Report(_progressCounter++);
				// try and merge the missing File list into the FileGroup classes
				// using the altCRC sorted list and then the CRCSorted list
				MergeInMissingFiles(fileGroupsCRCSorted, fileGroupsSHA1Sorted, fileGroupsMD5Sorted, fileGroupsAltCRCSorted, fileGroupsAltSHA1Sorted, fileGroupsAltMD5Sorted, filesMissing);

				var totalAfterMerge = fileGroupsCRCSorted.Length;

				_thWrk.Report(new bgwText("Finding Fixes"));
				_thWrk.Report(_progressCounter++);
				FindFixesListCheck.GroupListCheck(fileGroupsCRCSorted);

				_thWrk.Report(new bgwText("Complete (Unique Files " + totalAfterMerge + ")"));
				_thWrk.Finished = true;
				_thWrk = null;
			} catch (Exception exc) {
				ReportError.UnhandledExceptionHandler(exc);

				_thWrk?.Report(new bgwText("Updating Cache"));
				DB.Write();
				_thWrk?.Report(new bgwText("Complete"));
				if (_thWrk != null) {
					_thWrk.Finished = true;
				}

				_thWrk = null;
			}
		}

		private static void ResetFileGroups(RvFile tBase) {
			tBase.FileGroup = null;
			for (var i = 0; i < tBase.ChildCount; i++) {
				ResetFileGroups(tBase.Child(i));
			}
		}

		internal static void GetSelectedFiles(RvFile val, bool selected, List<RvFile> gotFiles, List<RvFile> missingFiles) {
			if (selected) {
				var rvFile = val;
				if (rvFile.IsFile) {
					switch (rvFile.GotStatus) {
						case GotStatus.Got:
						case GotStatus.Corrupt:
							gotFiles.Add(rvFile);
							break;
						case GotStatus.NotGot:
							missingFiles.Add(rvFile);
							break;
					}
				}
			}

			var rvVal = val;
			if (!rvVal.IsDir) {
				return;
			}

			for (var i = 0; i < rvVal.ChildCount; i++) {
				var nextSelect = selected;
				if (rvVal.Tree != null) {
					nextSelect = rvVal.Tree.Checked != RvTreeRow.TreeSelect.UnSelected;
				}

				GetSelectedFiles(rvVal.Child(i), nextSelect, gotFiles, missingFiles);
			}
		}

		// we are adding got files so we can assume a few things:
		//  these got files may be level 1 or level 2 scanned.
		//
		//  If they are just level 1 then there may or may not be SHA1 / MD5 info, which is unvalidated.
		//  So: We will always have CRC & Size info at a minimum and may also have SHA1 / MD5
		//
		//  Next: Due to the possilibity of CRC hash collisions 
		//  we could find matching CRC that have different SHA1 & MD5
		//  so we should seach for one or more matching CRC/Size sets.
		//  then check to see if we have anything else that matches and then either:
		//  add the rom to an existing set or make a new set.

		internal static void MergeGotFiles(RvFile[] gotFilesSortedByCRC, out FileGroup[] fileGroups) {
			var listFileGroupsOut = new List<FileGroup>();

			// insert a zero byte file.
			var fileZero = MakeFileZero();
			var newFileGroup = new FileGroup(fileZero);

			var lstFileWithSameCRC = new List<FileGroup>();
			var crc = fileZero.CRC;

			lstFileWithSameCRC.Add(newFileGroup);
			listFileGroupsOut.Add(newFileGroup);

			foreach (var file in gotFilesSortedByCRC) {
				if (file.CRC == null) {
					continue;
				}

				if (crc != null && ArrByte.ICompare(crc, file.CRC) == 0) {
					var found = false;
					foreach (var fileGroup in lstFileWithSameCRC) {
						if (!fileGroup.FindExactMatch(file)) {
							continue;
						}

						fileGroup.MergeFileIntoGroup(file);
						found = true;
						break;
					}

					if (found) {
						continue;
					}

					// new File with the same CRC but different sha1/md5/size
					newFileGroup = new FileGroup(file);
					lstFileWithSameCRC.Add(newFileGroup);
					listFileGroupsOut.Add(newFileGroup);
					continue;
				}

				crc = file.CRC;
				lstFileWithSameCRC.Clear();
				newFileGroup = new FileGroup(file);
				lstFileWithSameCRC.Add(newFileGroup);
				listFileGroupsOut.Add(newFileGroup);
			}

			fileGroups = listFileGroupsOut.ToArray();
		}

		private static void MergeInMissingFiles(FileGroup[] mergedCRCFamily, FileGroup[] mergedSHA1Family, FileGroup[] mergedMD5Family,
												FileGroup[] mergedAltCRCFamily, FileGroup[] mergedAltSHA1Family, FileGroup[] mergedAltMD5Family, List<RvFile> missingFiles) {
			foreach (var f in missingFiles) {
				//first try and match on CRC
				//if (f.CRC != null)
				//{
				if (f.AltSize != null || f.AltCRC != null || f.AltSHA1 != null || f.AltMD5 != null) {
					throw new InvalidOperationException("Missing files cannot have alt values");
				}

				if (f.HeaderFileType != HeaderFileType.Nothing) {

					if (f.CRC != null) {
						var found = FindMissingOnAlt(f, CompareAltCRC, mergedAltCRCFamily);
						if (found) {
							continue;
						}
					}

					if (f.SHA1 != null) {
						var found = FindMissingOnAlt(f, CompareAltSHA1, mergedAltSHA1Family);
						if (found) {
							continue;
						}
					}

					if (f.MD5 != null) {
						var found = FindMissingOnAlt(f, CompareAltMD5, mergedAltMD5Family);
						if (found) {
							continue;
						}
					}
				}

				if (f.CRC != null) {
					var found = FindMissing(f, CompareCRC, mergedCRCFamily);
					if (found) {
						continue;
					}
				}

				if (f.SHA1 != null) {
					var found = FindMissing(f, CompareSHA1, mergedSHA1Family);
					if (found) {
						continue;
					}
				}

				if (f.MD5 != null) {
					var found = FindMissing(f, CompareMD5, mergedMD5Family);
					if (found) {
						continue;
					}
				}

				if (f.CRC == null && f.SHA1 == null && f.MD5 == null) {

					if (f.Size == 0) {
						mergedCRCFamily[0].MergeFileIntoGroup(f);
					} else if (f.Size == null && f.Name.Length > 1 && f.Name.Substring(f.Name.Length - 1, 1) == "/") {
						mergedCRCFamily[0].MergeFileIntoGroup(f);
					}
				}
			}
		}

		private static bool FindMissing(RvFile f, Compare comp, FileGroup[] mergedFamily) {
			var found = FindMatch(mergedFamily, f, comp, FileGroup.FindExactMatch, out var index);

			if (index.Count > 1) {
				// if there is more than one exact match this means there is kind of a big mess going on, and things should
				// probably be level 2 scanned, will just use the first found set, but should probably report in the error log
				// that things are not looking good.
			}

			if (!found) {
				return false;
			}

			mergedFamily[index[0]].MergeFileIntoGroup(f);
			return true;

		}

		private static bool FindMissingOnAlt(RvFile f, Compare comp, FileGroup[] mergedFamily) {
			var found = FindMatch(mergedFamily, f, comp, FileGroup.FindAltExactMatch, out var index);
			if (!found) {
				return false;
			}

			mergedFamily[index[0]].MergeAltFileIntoGroup(f);
			return true;

		}

		internal delegate bool ExactMatch(FileGroup fTest, RvFile file);
		internal static bool FindMatch(FileGroup[] fileGroups, RvFile file, Compare comp, ExactMatch match, out List<int> listIndex) {
			var intBottom = 0;
			var intTop = fileGroups.Length;
			var intMid = 0;
			var intRes = -1;

			//Binary chop to find the closest match
			while ((intBottom < intTop) && (intRes != 0)) {
				intMid = (intBottom + intTop) / 2;

				var ff = fileGroups[intMid];
				intRes = comp(file, ff);
				if (intRes < 0) {
					intTop = intMid;
				} else if (intRes > 0) {
					intBottom = intMid + 1;
				}
			}

			var index = intMid;

			listIndex = new List<int>();

			// if match was found check up the list for the first match
			if (intRes == 0) {
				var intRes1 = 0;
				while (index > 0 && intRes1 == 0) {
					var ff = fileGroups[index - 1];
					intRes1 = comp(file, ff);

					if (intRes1 != 0) {
						continue;
					}

					index--;
				}

				var indexFirst = index;

				intTop = fileGroups.Length;
				intRes1 = 0;
				while (index < intTop && intRes1 == 0) {
					var ff = fileGroups[index];
					intRes1 = comp(file, ff);
					if (intRes1 != 0) {
						continue;
					}

					if (match(ff, file)) {
						listIndex.Add(index);
					}

					index++;
				}

				if (listIndex.Count == 0) {
					listIndex.Add(indexFirst);
					intRes = -1;
				}
			}
			// if the search is greater than the closest match move one up the list
			else {
				if (intRes > 0) {
					index++;
				}

				listIndex.Add(index);
			}

			return intRes == 0;
		}

		private static RvFile MakeFileZero() {
			var fileZero = new RvFile(FileType.File) {
				Name = "ZeroFile",
				Size = 0,
				CRC = new byte[] { 0, 0, 0, 0 }
			};

			fileZero.CRC = VarFix.CleanMD5SHA1("00000000", 8);
			fileZero.MD5 = VarFix.CleanMD5SHA1("d41d8cd98f00b204e9800998ecf8427e", 32);
			fileZero.SHA1 = VarFix.CleanMD5SHA1("da39a3ee5e6b4b0d3255bfef95601890afd80709", 40);

			fileZero.GotStatus = GotStatus.Got;
			fileZero.DatStatus = DatStatus.InToSort;
			return fileZero;
		}

		internal delegate int Compare(RvFile file, FileGroup fileGroup);

		private static int CompareCRC(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.CRC, fileGroup.CRC);
		private static int CompareSHA1(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.SHA1, fileGroup.SHA1);
		private static int CompareMD5(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.MD5, fileGroup.MD5);
		private static int CompareAltCRC(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.CRC, fileGroup.AltCRC);
		private static int CompareAltSHA1(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.SHA1, fileGroup.AltSHA1);
		private static int CompareAltMD5(RvFile file, FileGroup fileGroup) => ArrByte.ICompare(file.MD5, fileGroup.AltMD5);

		private static bool FindSHA1(FileGroup fileGroup) => fileGroup.SHA1 != null;
		private static bool FindMD5(FileGroup fileGroup) => fileGroup.MD5 != null;
		private static bool FindAltCRC(FileGroup fileGroup) => fileGroup.AltCRC != null;
		private static bool FindAltSHA1(FileGroup fileGroup) => fileGroup.AltSHA1 != null;
		private static bool FindAltMD5(FileGroup fileGroup) => fileGroup.AltMD5 != null;

		private static int FamilySortSHA1(FileGroup fileGroup1, FileGroup fileGroup2) => ArrByte.ICompare(fileGroup1.SHA1, fileGroup2.SHA1);
		private static int FamilySortMD5(FileGroup fileGroup1, FileGroup fileGroup2) => ArrByte.ICompare(fileGroup1.MD5, fileGroup2.MD5);
		private static int FamilySortAltCRC(FileGroup fileGroup1, FileGroup fileGroup2) => ArrByte.ICompare(fileGroup1.AltCRC, fileGroup2.AltCRC);
		private static int FamilySortAltSHA1(FileGroup fileGroup1, FileGroup fileGroup2) => ArrByte.ICompare(fileGroup1.AltSHA1, fileGroup2.AltSHA1);
		private static int FamilySortAltMD5(FileGroup fileGroup1, FileGroup fileGroup2) => ArrByte.ICompare(fileGroup1.AltMD5, fileGroup2.AltMD5);

	}
}
