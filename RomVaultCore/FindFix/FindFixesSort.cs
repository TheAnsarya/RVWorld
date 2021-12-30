﻿using System;
using System.Collections.Generic;
using System.Threading;
using RomVaultCore.RvDB;
using RomVaultCore.Utils;

namespace RomVaultCore.FindFix {
	public static class FindFixesSort {
		public delegate bool FindOn(FileGroup fileGroup);
		public delegate int SortOn(FileGroup fileGroup1, FileGroup fileGroup2);

		public static RvFile[] SortCRC(List<RvFile> files) {
			var sortedCRC = files.ToArray();
			SortCRC(0, sortedCRC.Length, sortedCRC, 0);
			return sortedCRC;
		}

		private static void SortCRC(int intBase, int intTop, RvFile[] files, int depth) {
			var sortSize = intTop - intBase;
			if (sortSize <= 1) {
				return;
			}

			// if just 2 tests 
			if (sortSize == 2) {
				// compare the 2 files
				var t0 = files[intBase];
				var t1 = files[intBase + 1];
				if (ArrByte.ICompare(t0.CRC, t1.CRC) < 1) {
					return;
				}
				// swap them
				files[intBase] = t1;
				files[intBase + 1] = t0;
				return;
			}

			var intMiddle = (intTop + intBase) / 2;

			if (depth < 2) {
				var t0 = new Thread(() => SortCRC(intBase, intMiddle, files, depth + 1));
				var t1 = new Thread(() => SortCRC(intMiddle, intTop, files, depth + 1));
				t0.Start();
				t1.Start();
				t0.Join();
				t1.Join();
			} else {
				SortCRC(intBase, intMiddle, files, depth + 1);
				SortCRC(intMiddle, intTop, files, depth + 1);
			}

			var intBottomSize = intMiddle - intBase;
			var intTopSize = intTop - intMiddle;

			var lstBottom = new RvFile[intBottomSize];
			var lstTop = new RvFile[intTopSize];

			if (depth == 0) {
				var t0 = new Thread(() => Array.Copy(files, intBase, lstBottom, 0, intBottomSize));
				var t1 = new Thread(() => Array.Copy(files, intMiddle, lstTop, 0, intTopSize));
				t0.Start();
				t1.Start();
				t0.Join();
				t1.Join();
			} else {
				Array.Copy(files, intBase, lstBottom, 0, intBottomSize);
				Array.Copy(files, intMiddle, lstTop, 0, intTopSize);
			}

			var intBottomCount = 0;
			var intTopCount = 0;
			var intCount = intBase;

			while (intBottomCount < intBottomSize && intTopCount < intTopSize) {
				if (ArrByte.ICompare(lstBottom[intBottomCount].CRC, lstTop[intTopCount].CRC) < 1) {
					files[intCount++] = lstBottom[intBottomCount++];
				} else {
					files[intCount++] = lstTop[intTopCount++];
				}
			}

			while (intBottomCount < intBottomSize) {
				files[intCount++] = lstBottom[intBottomCount++];
			}

			while (intTopCount < intTopSize) {
				files[intCount++] = lstTop[intTopCount++];
			}
		}

		public static void SortFamily(FileGroup[] mergedFamily, FindOn find, SortOn sort, out FileGroup[] outArray) {
			var outList = new List<FileGroup>();
			foreach (var fm in mergedFamily) {
				if (find(fm)) {
					outList.Add(fm);
				}
			}

			outArray = outList.ToArray();

			SortFamily(0, outArray.Length, outArray, sort, 0);
		}

		private static void SortFamily(int intBase, int intTop, FileGroup[] arrFamily, SortOn sortFunction, int depth) {
			var sortSize = intTop - intBase;
			if (sortSize <= 1) {
				return;
			}

			// if just 2 tests 
			if (sortSize == 2) {
				// compare the 2 files
				var t0 = arrFamily[intBase];
				var t1 = arrFamily[intBase + 1];
				if (sortFunction(t0, t1) < 1) {
					return;
				}
				// swap them
				arrFamily[intBase] = t1;
				arrFamily[intBase + 1] = t0;
				return;
			}

			var intMiddle = (intTop + intBase) / 2;

			if (depth < 2) {
				var t0 = new Thread(() => SortFamily(intBase, intMiddle, arrFamily, sortFunction, depth + 1));
				var t1 = new Thread(() => SortFamily(intMiddle, intTop, arrFamily, sortFunction, depth + 1));
				t0.Start();
				t1.Start();
				t0.Join();
				t1.Join();
			} else {
				SortFamily(intBase, intMiddle, arrFamily, sortFunction, depth + 1);
				SortFamily(intMiddle, intTop, arrFamily, sortFunction, depth + 1);
			}

			var intBottomSize = intMiddle - intBase;
			var intTopSize = intTop - intMiddle;

			var arrBottom = new FileGroup[intBottomSize];
			var arrTop = new FileGroup[intTopSize];

			if (depth == 0) {
				var t0 = new Thread(() => Array.Copy(arrFamily, intBase, arrBottom, 0, intBottomSize));
				var t1 = new Thread(() => Array.Copy(arrFamily, intMiddle, arrTop, 0, intTopSize));
				t0.Start();
				t1.Start();
				t0.Join();
				t1.Join();
			} else {
				Array.Copy(arrFamily, intBase, arrBottom, 0, intBottomSize);
				Array.Copy(arrFamily, intMiddle, arrTop, 0, intTopSize);
			}

			var intBottomCount = 0;
			var intTopCount = 0;
			var intCount = intBase;

			while (intBottomCount < intBottomSize && intTopCount < intTopSize) {
				if (sortFunction(arrBottom[intBottomCount], arrTop[intTopCount]) < 1) {
					arrFamily[intCount++] = arrBottom[intBottomCount++];
				} else {
					arrFamily[intCount++] = arrTop[intTopCount++];
				}
			}

			while (intBottomCount < intBottomSize) {
				arrFamily[intCount++] = arrBottom[intBottomCount++];
			}

			while (intTopCount < intTopSize) {
				arrFamily[intCount++] = arrTop[intTopCount++];
			}
		}
	}
}
