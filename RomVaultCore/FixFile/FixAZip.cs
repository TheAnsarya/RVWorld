using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Compress;
using RomVaultCore.FixFile.Util;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;
using RVIO;

namespace RomVaultCore.FixFile {
	public static class FixAZip {
		public class ZipFileException : Exception {
			public ZipFileException(ReturnCode rc, string message) : base(message) => returnCode = rc;

			public ReturnCode returnCode { get; }
		}

		public static ReturnCode FixZip(RvFile fixZip, List<RvFile> fileProcessQueue, ref int totalFixed, out string errorMessage) {
			errorMessage = "";

			//Check for error status
			if (fixZip.DirStatus.HasUnknown()) {
				return ReturnCode.FindFixes; // Error
			}
			var needsTrrntzipped = fixZip.ZipStatus != ZipStatus.TrrntZip && fixZip.GotStatus == GotStatus.Got && fixZip.DatStatus == DatStatus.InDatCollect && Settings.rvSettings.ConvertToTrrntzip;

			// file corrupt and not in tosort
			//      if file cannot be fully fixed copy to corrupt
			//      process zipfile

			if (fixZip.GotStatus == GotStatus.Corrupt && fixZip.DatStatus != DatStatus.InToSort && !fixZip.DirStatus.HasFixable()) {
				var moveReturnCode = FixAZipFunctions.MoveZipToCorrupt(fixZip, out errorMessage);
				if (moveReturnCode != ReturnCode.Good) {
					return moveReturnCode;
				}
			}

			// has fixable
			//      process zipfile

			else if (fixZip.DirStatus.HasFixable()) {
				// do nothing here but continue on to process zip.
			}

			// need trrntzipped
			//      process zipfile

			else if (needsTrrntzipped) {
				// rv7Zip format is not finalized yet so do not use
				if (!Settings.rvSettings.ConvertToRV7Z && (fixZip.FileType == FileType.SevenZip))
				//if (fixZip.FileType == FileType.SevenZip)
				{
					needsTrrntzipped = false;
				}
				// do nothing here but continue on to process zip.
			}

			// got empty zip that should be deleted
			//      process zipfile
			else if (fixZip.GotStatus == GotStatus.Got && fixZip.GotStatus != GotStatus.Corrupt && !fixZip.DirStatus.HasAnyFiles()) {
				// do nothing here but continue on to process zip.
			}

			// else
			//      skip this zipfile
			else {
				// nothing can be done to return
				return ReturnCode.Good;
			}

			if (!fixZip.DirStatus.HasFixable() && !needsTrrntzipped) {
				return ReturnCode.Good;
			}

			var fixZipFullName = fixZip.TreeFullName;

			if (!fixZip.DirStatus.HasFixable() && needsTrrntzipped) {
				Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), "", 0, "TrrntZipping", "", "", ""));
			}

			FixFileUtils.CheckCreateDirectories(fixZip.Parent);

			var filename = fixZip.FullName;
			if (fixZip.GotStatus == GotStatus.NotGot) {
				if (File.Exists(filename)) {
					errorMessage = "Unexpected file found in directory. Rescan needed.\n" + filename;
					return ReturnCode.RescanNeeded;
				}
			}

			if (Settings.rvSettings.DebugLogsEnabled) {
				ReportError.LogOut("");
				ReportError.LogOut(fixZipFullName + " : " + fixZip.RepStatus);
				ReportError.LogOut("------------------------------------------------------------");
				Debug.WriteLine(fixZipFullName + " : " + fixZip.RepStatus);
				ReportError.LogOut("Zip File Status Before Fix:");

				var deleteCount = 0;
				var moveToSortCount = 0;
				var notThereCount = 0;
				for (var intLoop = 0; intLoop < fixZip.ChildCount; intLoop++) {
					var fixZipFile = fixZip.Child(intLoop);
					ReportError.LogOut(fixZipFile);
					if (fixZipFile.RepStatus == RepStatus.MoveToSort || fixZipFile.RepStatus == RepStatus.MoveToCorrupt) {
						moveToSortCount++;
					}

					if (fixZipFile.RepStatus == RepStatus.Delete) {
						deleteCount++;
					}

					if (fixZipFile.RepStatus == RepStatus.Missing || fixZip.RepStatus == RepStatus.NotCollected) {
						notThereCount++;
					}
				}
				ReportError.LogOut($"MoveToSortCount {moveToSortCount} , DeleteCount {deleteCount} , NotThereCount {notThereCount}");
				ReportError.LogOut("");
			}

			var returnCode = ReturnCode.Good;
			var fileRepStatus = RepStatus.UnSet;

			ICompress tempFixZip = null;
			ICompress toSortCorruptOut = null;
			ICompress toSortZipOut = null;
			try {
				RvFile toSortGame = null;
				RvFile toSortCorruptGame = null;

				var filesUsedForFix = new Dictionary<string, RvFile>();
				var fixZipTemp = new List<RvFile>();

				var fixFileType = fixZip.FileType;

				for (var iRom = 0; iRom < fixZip.ChildCount; iRom++) {
					var fixZippedFile = new RvFile(DBTypeGet.FileFromDir(fixFileType));
					fixZip.Child(iRom).CopyTo(fixZippedFile);

					fixZipTemp.Add(fixZippedFile);

					ReportError.LogOut(fixZippedFile.RepStatus + " : " + fixZip.Child(iRom).FullName);

					fileRepStatus = fixZippedFile.RepStatus;
					switch (fixZippedFile.RepStatus) {
						#region Nothing to copy

						// any file we do not have or do not want in the destination zip
						case RepStatus.Missing:
						case RepStatus.NotCollected:
						case RepStatus.Rename:
						case RepStatus.Delete:
							if (!
								(
									// got the file in the original zip but will be deleting it
									(fixZippedFile.DatStatus == DatStatus.NotInDat && fixZippedFile.GotStatus == GotStatus.Got) ||
									(fixZippedFile.DatStatus == DatStatus.NotInDat && fixZippedFile.GotStatus == GotStatus.Corrupt) ||
									(fixZippedFile.DatStatus == DatStatus.InDatMerged && fixZippedFile.GotStatus == GotStatus.Got) ||
									(fixZippedFile.DatStatus == DatStatus.InToSort && fixZippedFile.GotStatus == GotStatus.Got) ||
									(fixZippedFile.DatStatus == DatStatus.InToSort && fixZippedFile.GotStatus == GotStatus.Corrupt) ||

									// do not have this file and cannot fix it here
									(fixZippedFile.DatStatus == DatStatus.InDatCollect && fixZippedFile.GotStatus == GotStatus.NotGot) ||
									(fixZippedFile.DatStatus == DatStatus.InDatBad && fixZippedFile.GotStatus == GotStatus.NotGot) ||
									(fixZippedFile.DatStatus == DatStatus.InDatMerged && fixZippedFile.GotStatus == GotStatus.NotGot)
								)
							) {
								ReportError.SendAndShow($"Error in Fix Rom Status {fixZippedFile.RepStatus} : {fixZippedFile.DatStatus} : {fixZippedFile.GotStatus}");
							}

							if (fixZippedFile.RepStatus == RepStatus.Delete) {
								if (Settings.rvSettings.DetailedFixReporting) {
									Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), fixZippedFile.Name, fixZippedFile.Size, "Delete", "", "", ""));
								}

								returnCode = FixFileUtils.DoubleCheckDelete(fixZip.Child(iRom), out errorMessage);
								if (returnCode != ReturnCode.Good) {
									CloseZipFile(ref tempFixZip);
									CloseToSortGame(toSortGame, ref toSortZipOut);
									CloseToSortCorruptGame(toSortGame, ref toSortZipOut);
									return returnCode;
								}
							}

							fixZippedFile.GotStatus = GotStatus.NotGot;
							break;

						#endregion

						// any files we are just moving from the original zip to the destination zip
						case RepStatus.Correct:
						case RepStatus.InToSort:
						case RepStatus.NeededForFix:
						case RepStatus.Corrupt: {
								returnCode = FixAZipCorrectZipFile.CorrectZipFile(fixZip, fixZippedFile, ref tempFixZip, iRom, filesUsedForFix, out errorMessage);
								if (returnCode != ReturnCode.Good) {
									CloseZipFile(ref tempFixZip);
									CloseToSortGame(toSortGame, ref toSortZipOut);
									CloseToSortCorruptGame(toSortGame, ref toSortZipOut);
									return returnCode;
								}
								break;
							}

						case RepStatus.CanBeFixed:
						case RepStatus.CorruptCanBeFixed: {
								returnCode = FixAZipCanBeFixed.CanBeFixed(fixZip, fixZippedFile, ref tempFixZip, filesUsedForFix, ref totalFixed, out errorMessage);
								if (returnCode != ReturnCode.Good) {
									CloseZipFile(ref tempFixZip);
									CloseToSortGame(toSortGame, ref toSortZipOut);
									CloseToSortCorruptGame(toSortGame, ref toSortZipOut);
									return returnCode;
								}
								break;
							}

						case RepStatus.MoveToSort: {
								returnCode = FixAZipMoveToSort.MovetoSort(fixZip, fixZippedFile, ref toSortGame, ref toSortZipOut, iRom, filesUsedForFix);
								if (returnCode != ReturnCode.Good) {
									CloseZipFile(ref tempFixZip);
									CloseToSortGame(toSortGame, ref toSortZipOut);
									CloseToSortCorruptGame(toSortGame, ref toSortZipOut);
									return returnCode;
								}
								break;
							}

						case RepStatus.MoveToCorrupt:
							returnCode = FixAZipFunctions.MoveToCorrupt(fixZip, fixZippedFile, ref toSortCorruptGame, ref toSortCorruptOut, iRom);
							if (returnCode != ReturnCode.Good) {
								CloseZipFile(ref tempFixZip);
								CloseToSortGame(toSortGame, ref toSortZipOut);
								CloseToSortCorruptGame(toSortGame, ref toSortZipOut);
								return returnCode;
							}
							break;

						default:
							ReportError.UnhandledExceptionHandler($"Unknown file status found {fixZippedFile.RepStatus} while fixing file {fixZip.Name} Dat Status = {fixZippedFile.DatStatus} GotStatus {fixZippedFile.GotStatus}");
							break;
					}

					if (Report.CancellationPending()) {
						tempFixZip?.ZipFileCloseFailed();
						toSortZipOut?.ZipFileCloseFailed();
						toSortCorruptOut?.ZipFileCloseFailed();
						tempFixZip = null;
						toSortZipOut = null;
						toSortCorruptOut = null;

						errorMessage = "Cancel";
						return ReturnCode.Cancel;
					}
				}

				//if ToSort Zip Made then close the zip and add this new zip to the Database
				CloseToSortGame(toSortGame, ref toSortZipOut);

				//if Corrupt Zip Made then close the zip and add this new zip to the Database
				CloseToSortCorruptGame(toSortCorruptGame, ref toSortCorruptOut);

				#region Process original Zip

				if (File.Exists(filename)) {
					if (!File.SetAttributes(filename, FileAttributes.Normal)) {
						var error = Error.GetLastError();
						Report.ReportProgress(new bgwShowError(filename, $"Error Setting File Attributes to Normal. Deleting Original Fix File. Code {error}"));
					}

					try {
						File.Delete(filename);
					} catch (Exception e) {
						errorMessage = $"Error While trying to delete file {filename}. {e.Message}";

						if (tempFixZip != null && tempFixZip.ZipOpen != ZipOpenType.Closed) {
							tempFixZip.ZipFileClose();
							tempFixZip = null;
						}

						return ReturnCode.RescanNeeded;
					}
				}

				#endregion

				var checkDelete = false;

				#region process the temp Zip rename it to the original Zip

				if (tempFixZip != null && tempFixZip.ZipOpen != ZipOpenType.Closed) {
					var tempFilename = tempFixZip.ZipFilename;
					tempFixZip.ZipFileClose();

					if (tempFixZip.LocalFilesCount() > 0) {
						// now rename the temp fix file to the correct filename
						File.Move(tempFilename, filename);
						var nFile = new FileInfo(filename);
						var tmpZip = new RvFile(FileType.Zip) {
							Name = Path.GetFileName(filename),
							FileModTimeStamp = nFile.LastWriteTime
						};
						tmpZip.SetStatus(fixZip.DatStatus, GotStatus.Got);

						fixZip.FileAdd(tmpZip, false);
						fixZip.ZipStatus = tempFixZip.ZipStatus;
					} else {
						File.Delete(tempFilename);
						checkDelete = true;
					}

					tempFixZip = null;
				} else {
					checkDelete = true;
				}

				#endregion

				#region Now put the New Game Status information into the Database.

				var intLoopFix = 0;
				foreach (var tmpZip in fixZipTemp) {
					tmpZip.CopyTo(fixZip.Child(intLoopFix));

					if (fixZip.Child(intLoopFix).RepStatus == RepStatus.Deleted) {
						if (fixZip.Child(intLoopFix).FileRemove() == EFile.Delete) {
							fixZip.ChildRemove(intLoopFix);
							continue;
						}
					}

					intLoopFix++;
				}

				#endregion

				var usedFiles = filesUsedForFix.Values.ToList();

				FixFileUtils.CheckFilesUsedForFix(usedFiles, fileProcessQueue, false);

				if (checkDelete) {
					FixFileUtils.CheckDeleteFile(fixZip);
				}

				ReportError.LogOut("");
				ReportError.LogOut("Zip File Status After Fix:");
				for (var intLoop = 0; intLoop < fixZip.ChildCount; intLoop++) {
					ReportError.LogOut(fixZip.Child(intLoop));
				}

				ReportError.LogOut("");

				return ReturnCode.Good;

			} catch (ZipFileException ex) {
				tempFixZip?.ZipFileCloseFailed();
				toSortZipOut?.ZipFileCloseFailed();
				toSortCorruptOut?.ZipFileCloseFailed();
				tempFixZip = null;
				toSortZipOut = null;
				toSortCorruptOut = null;

				errorMessage = ex.Message;
				return ex.returnCode;
			} catch (Exception ex) {
				tempFixZip?.ZipFileCloseFailed();
				toSortZipOut?.ZipFileCloseFailed();
				toSortCorruptOut?.ZipFileCloseFailed();
				tempFixZip = null;
				toSortZipOut = null;
				toSortCorruptOut = null;

				errorMessage = ex.Message;
				return ReturnCode.LogicError;
			} finally {
				if (tempFixZip != null) {
					ReportError.UnhandledExceptionHandler($"{tempFixZip.ZipFilename} tempZipOut was left open, ZipFile= {fixZipFullName} , fileRepStatus= {fileRepStatus} , returnCode= {returnCode}");
				}
				if (toSortZipOut != null) {
					ReportError.UnhandledExceptionHandler($"{toSortZipOut.ZipFilename} toSortZipOut was left open");
				}
				if (toSortCorruptOut != null) {
					ReportError.UnhandledExceptionHandler($"{toSortCorruptOut.ZipFilename} toSortCorruptOut was left open");
				}
			}
		}

		private static void CloseZipFile(ref ICompress tempFixZip) {
			tempFixZip?.ZipFileCloseFailed();
			tempFixZip = null;
		}

		private static void CloseToSortGame(RvFile toSortGame, ref ICompress toSortZipOut) {
			if (toSortGame != null) {
				toSortZipOut.ZipFileClose();

				toSortGame.FileModTimeStamp = toSortZipOut.TimeStamp;
				toSortGame.DatStatus = DatStatus.InToSort;
				toSortGame.GotStatus = GotStatus.Got;
				toSortGame.ZipStatus = toSortZipOut.ZipStatus;
				toSortZipOut = null;

				var toSort = toSortGame.Parent;
				toSort.ChildAdd(toSortGame);
			}
		}

		private static void CloseToSortCorruptGame(RvFile toSortCorruptGame, ref ICompress toSortCorruptOut) {
			if (toSortCorruptGame != null) {
				toSortCorruptOut.ZipFileClose();

				toSortCorruptGame.FileModTimeStamp = toSortCorruptOut.TimeStamp;
				toSortCorruptGame.DatStatus = DatStatus.InToSort;
				toSortCorruptGame.GotStatus = GotStatus.Got;

				var toSort = DB.RvFileToSort();
				var corruptDir = new RvFile(FileType.Dir) { Name = "Corrupt", DatStatus = DatStatus.InToSort };
				var found = toSort.ChildNameSearch(corruptDir, out var indexCorrupt);
				if (found != 0) {
					corruptDir.GotStatus = GotStatus.Got;
					indexCorrupt = toSort.ChildAdd(corruptDir);
				}

				toSort.Child(indexCorrupt).ChildAdd(toSortCorruptGame);
				toSortCorruptOut = null;
			}
		}
	}
}
