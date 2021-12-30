﻿using RomVaultCore.FindFix;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;
using File = RVIO.File;
using FileInfo = RVIO.FileInfo;


namespace RomVaultCore.FixFile.Util {


	public static partial class FixFileUtils {
		public static ReturnCode MoveFile(RvFile fileIn, RvFile fileOut, string outFilename, out bool fileMoved, out string error) {
			error = "";
			fileMoved = false;

			var fileMove = TestFileMove(fileIn, fileOut);

			if (!fileMove) {
				return ReturnCode.Good;
			}

			byte[] bCRC;
			byte[] bMD5 = null;
			byte[] bSHA1 = null;

			var fileNameIn = fileIn.FullName;
			if (!File.Exists(fileNameIn)) {
				error = "Rescan needed, File Not Found :" + fileNameIn;
				return ReturnCode.RescanNeeded;
			}
			var fileInInfo = new FileInfo(fileNameIn);
			if (fileInInfo.LastWriteTime != fileIn.FileModTimeStamp) {
				error = "Rescan needed, File Changed :" + fileNameIn;
				return ReturnCode.RescanNeeded;
			}

			var fileNameOut = outFilename ?? fileOut.FullName;
			File.Move(fileNameIn, fileNameOut);

			bCRC = fileIn.CRC.Copy();
			if (fileIn.FileStatusIs(FileStatus.MD5Verified)) {
				bMD5 = fileIn.MD5.Copy();
			}

			if (fileIn.FileStatusIs(FileStatus.SHA1Verified)) {
				bSHA1 = fileIn.SHA1.Copy();
			}

			var fi = new FileInfo(fileNameOut);
			fileOut.FileModTimeStamp = fi.LastWriteTime;

			var retC = ValidateFileOut(fileIn, fileOut, true, bCRC, bSHA1, bMD5, out error);
			if (retC != ReturnCode.Good) {
				return retC;
			}

			CheckDeleteFile(fileIn);

			fileMoved = true;
			return ReturnCode.Good;
		}

		public static bool TestFileMove(RvFile fileIn, RvFile fileOut) {
			if (fileIn.FileType != FileType.File) {
				return false;
			}

			if (fileOut.FileType != FileType.File) {
				return false;
			}

			if (FindFixesListCheck.treeType(fileIn) == RvTreeRow.TreeSelect.Locked) {
				return false;
			}

			if (fileIn.RepStatus == RepStatus.NeededForFix || fileIn.RepStatus == RepStatus.MoveToSort || fileIn.RepStatus == RepStatus.Rename) {
				return true;
			}

			return false;
		}

	}
}