using CHDlib;
using Compress;
using Compress.SevenZip;
using Compress.ZipFile;
using FileHeaderReader;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;
using DirectoryInfo = RVIO.DirectoryInfo;
using File = RVIO.File;
using Path = RVIO.Path;

namespace RomVaultCore.Scanner {
	public static class Populate {
		private static FileScan _fs;

		public static RvFile FromAZipFile(RvFile dbDir, EScanLevel eScanLevel, ThreadWorker thWrk) {
			var fileDir = new RvFile(dbDir.FileType);
			var chechingDatStatus = dbDir.IsInToSort ? DatStatus.InToSort : DatStatus.NotInDat;

			var filename = dbDir.FullNameCase;
			var checkZ = dbDir.FileType == FileType.Zip ? new Zip() : (ICompress)new SevenZ();
			var zr = checkZ.ZipFileOpen(filename, dbDir.FileModTimeStamp);

			if (zr == ZipReturn.ZipGood) {
				dbDir.ZipStatus = checkZ.ZipStatus;

				// to be Scanning a ZIP file means it is either new or has changed.
				// as the code below only calls back here if that is true.
				//
				// Level1: Only use header CRC's
				// Just get the CRC for the ZIP headers.
				//
				// Level2: Fully checksum changed only files
				// We know this file has been changed to do a full checksum scan.
				//
				// Level3: Fully checksum everything
				// So do a full checksum scan.
				if (_fs == null) {
					_fs = new FileScan();
				}

				var fr = _fs.Scan(checkZ, false, eScanLevel == EScanLevel.Level2 || eScanLevel == EScanLevel.Level3);

				// add all of the file information from the zip file into scanDir
				for (var i = 0; i < checkZ.LocalFilesCount(); i++) {
					var lf = checkZ.GetLocalFile(i);
					var tFile = new RvFile(DBTypeGet.FileFromDir(dbDir.FileType)) {
						Name = lf.Filename,
						ZipFileIndex = i,
						ZipFileHeaderPosition = lf.LocalHead,
						Size = lf.UncompressedSize,
						CRC = lf.CRC,
						FileModTimeStamp = lf.LastModified
					};
					// all levels read the CRC from the ZIP header
					tFile.SetStatus(chechingDatStatus, GotStatus.Got);
					tFile.FileStatusSet(FileStatus.SizeFromHeader | FileStatus.CRCFromHeader);

					if (fr[i].FileStatus != ZipReturn.ZipGood) {
						thWrk.Report(new bgwShowCorrupt(fr[i].FileStatus, filename + " : " + lf.Filename));
						tFile.GotStatus = GotStatus.Corrupt;
					} else {
						tFile.HeaderFileType = fr[i].HeaderFileType;
						tFile.SHA1 = fr[i].SHA1;
						tFile.MD5 = fr[i].MD5;
						tFile.AltSize = fr[i].AltSize;
						tFile.AltCRC = fr[i].AltCRC;
						tFile.AltSHA1 = fr[i].AltSHA1;
						tFile.AltMD5 = fr[i].AltMD5;

						if (fr[i].CRC != null) {
							tFile.FileStatusSet(FileStatus.CRCFromHeader);
							if (eScanLevel == EScanLevel.Level2 || eScanLevel == EScanLevel.Level3) {
								tFile.FileStatusSet(FileStatus.CRCVerified);
							}
						}

						tFile.FileStatusSet(
							FileStatus.SizeVerified |
							(fr[i].HeaderFileType != HeaderFileType.Nothing ? FileStatus.HeaderFileTypeFromHeader : 0) |
							(fr[i].SHA1 != null ? FileStatus.SHA1Verified : 0) |
							(fr[i].MD5 != null ? FileStatus.MD5Verified : 0) |
							(fr[i].AltSize != null ? FileStatus.AltSizeVerified : 0) |
							(fr[i].AltCRC != null ? FileStatus.AltCRCVerified : 0) |
							(fr[i].AltSHA1 != null ? FileStatus.AltSHA1Verified : 0) |
							(fr[i].AltMD5 != null ? FileStatus.AltMD5Verified : 0)
										 );
					}

					fileDir.ChildAdd(tFile);
				}
			} else if (zr == ZipReturn.ZipFileLocked) {
				thWrk.Report(new bgwShowError(filename, "Zip File Locked"));
				dbDir.FileModTimeStamp = 0;
				dbDir.GotStatus = GotStatus.FileLocked;
			} else if (zr == ZipReturn.ZipErrorOpeningFile) {
				thWrk.Report(new bgwShowError(filename, "Zip Error Opening File"));
				dbDir.FileModTimeStamp = 0;
				dbDir.GotStatus = GotStatus.FileLocked;
			} else {
				thWrk.Report(new bgwShowCorrupt(zr, filename));
				dbDir.GotStatus = GotStatus.Corrupt;
			}
			checkZ.ZipFileClose();

			return fileDir;
		}

		public static RvFile FromADir(RvFile dbDir, EScanLevel eScanLevel, ThreadWorker bgw, ref bool fileErrorAbort) {
			var fullDir = dbDir.FullNameCase;
			var datStatus = dbDir.IsInToSort ? DatStatus.InToSort : DatStatus.NotInDat;

			var fileDir = new RvFile(FileType.Dir);

			var oDir = new DirectoryInfo(fullDir);
			var oDirs = oDir.GetDirectories();
			var oFiles = oDir.GetFiles();

			// add all the subdirectories into scanDir 
			foreach (var dir in oDirs) {
				var tDir = new RvFile(FileType.Dir) {
					Name = dir.Name,
					FileModTimeStamp = dir.LastWriteTime
				};
				tDir.SetStatus(datStatus, GotStatus.Got);
				fileDir.ChildAdd(tDir);
			}

			// add all the files into scanDir
			foreach (var oFile in oFiles) {
				var fName = oFile.Name;
				if (fName == "__RomVault.tmp") {
					File.Delete(oFile.FullName);
					continue;
				}
				var fExt = Path.GetExtension(oFile.Name);

				var ft = DBTypeGet.fromExtention(fExt);

				if (Settings.rvSettings.FilesOnly) {
					ft = FileType.File;
				}

				var tFile = new RvFile(ft) {
					Name = oFile.Name,
					Size = (ulong)oFile.Length,
					FileModTimeStamp = oFile.LastWriteTime
				};
				tFile.FileStatusSet(FileStatus.SizeVerified);
				tFile.SetStatus(datStatus, GotStatus.Got);

				if (eScanLevel == EScanLevel.Level3 && tFile.FileType == FileType.File) {
					FromAFile(tFile, fullDir, eScanLevel, bgw, ref fileErrorAbort);
				}

				fileDir.ChildAdd(tFile);
			}
			return fileDir;
		}

		private static ThreadWorker _bgw;
		public static void FromAFile(RvFile file, string directory, EScanLevel eScanLevel, ThreadWorker bgw, ref bool fileErrorAbort) {
			_bgw = bgw;
			var filename = Path.Combine(directory, string.IsNullOrWhiteSpace(file.FileName) ? file.Name : file.FileName);
			ICompress fileToScan = new Compress.File.File();
			var zr = fileToScan.ZipFileOpen(filename, file.FileModTimeStamp);

			if (zr == ZipReturn.ZipFileLocked) {
				file.GotStatus = GotStatus.FileLocked;
				return;
			}

			if (zr != ZipReturn.ZipGood) {
				var error = zr.ToString();
				if (error.ToLower().StartsWith("zip")) {
					error = error.Substring(3);
				}

				ReportError.Show($"File: {filename} Error: {error}. Scan Aborted.");
				file.GotStatus = GotStatus.FileLocked;
				fileErrorAbort = true;
				return;
			}

			if (_fs == null) {
				_fs = new FileScan();
			}

			var fr = _fs.Scan(fileToScan, true, eScanLevel == EScanLevel.Level2 || eScanLevel == EScanLevel.Level3);

			file.HeaderFileType = fr[0].HeaderFileType;
			file.Size = fr[0].Size;
			file.CRC = fr[0].CRC;
			file.SHA1 = fr[0].SHA1;
			file.MD5 = fr[0].MD5;
			file.AltSize = fr[0].AltSize;
			file.AltCRC = fr[0].AltCRC;
			file.AltSHA1 = fr[0].AltSHA1;
			file.AltMD5 = fr[0].AltMD5;

			file.FileStatusSet(
				FileStatus.SizeVerified |
				(file.HeaderFileType != HeaderFileType.Nothing ? FileStatus.HeaderFileTypeFromHeader : 0) |
				(file.CRC != null ? FileStatus.CRCVerified : 0) |
				(file.SHA1 != null ? FileStatus.SHA1Verified : 0) |
				(file.MD5 != null ? FileStatus.MD5Verified : 0) |
				(file.AltSize != null ? FileStatus.AltSizeVerified : 0) |
				(file.AltCRC != null ? FileStatus.AltCRCVerified : 0) |
				(file.AltSHA1 != null ? FileStatus.AltSHA1Verified : 0) |
				(file.AltMD5 != null ? FileStatus.AltMD5Verified : 0)
			);

			if (fr[0].HeaderFileType == HeaderFileType.CHD) {
				var deepCheck = (eScanLevel == EScanLevel.Level2 || eScanLevel == EScanLevel.Level3);
				CHD.fileProcess = FileProcess;
				CHD.fileProgress = FileProgress;
				CHD.fileSystemError = FileSystemError;
				CHD.fileError = FileError;
				CHD.generalError = GeneralError;
				var result = CHD.CheckFile(file.Name, directory, Settings.isLinux, ref deepCheck, out var chdVersion, out var chdSHA1, out var chdMD5, ref fileErrorAbort);
				switch (result) {
					case hdErr.HDERR_NONE:
						file.CHDVersion = chdVersion;
						if (chdSHA1 != null) {
							file.AltSHA1 = chdSHA1;
							file.FileStatusSet(FileStatus.AltSHA1FromHeader);
							if (deepCheck) {
								file.FileStatusSet(FileStatus.AltSHA1Verified);
							}
						}

						if (chdMD5 != null) {
							file.AltMD5 = chdMD5;
							file.FileStatusSet(FileStatus.AltMD5FromHeader);
							if (deepCheck) {
								file.FileStatusSet(FileStatus.AltMD5Verified);
							}
						}
						break;

					case hdErr.HDERR_OUT_OF_MEMORY:
					case hdErr.HDERR_INVALID_FILE:
					case hdErr.HDERR_INVALID_DATA:
					case hdErr.HDERR_READ_ERROR:
					case hdErr.HDERR_DECOMPRESSION_ERROR:
					case hdErr.HDERR_CANT_VERIFY:
						file.GotStatus = GotStatus.Corrupt;
						break;

					default:
						ReportError.UnhandledExceptionHandler(result.ToString());
						break;
				}
			}
			fileToScan.ZipFileClose();

		}

		private static void FileProcess(string filename) => _bgw.Report(new bgwText2(filename));
		private static void FileProgress(string status) => _bgw.Report(new bgwText3(status));
		private static void FileSystemError(string status) => ReportError.Show(status);
		private static void FileError(string filename, string error) => _bgw.Report(new bgwShowError(filename, error));

		private static void GeneralError(string error) => ReportError.UnhandledExceptionHandler(error);
	}
}
