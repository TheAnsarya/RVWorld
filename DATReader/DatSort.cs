﻿using System;

namespace DATReader {
	public static class DatSort {

		public static int TrrntZipStringCompare(string string1, string string2) {
			var pos1 = 0;
			var pos2 = 0;

			for (; ; )
			{
				if (pos1 == string1.Length) {
					return pos2 == string2.Length ? 0 : -1;
				}
				if (pos2 == string2.Length) {
					return 1;
				}

				int byte1 = string1[pos1++];
				int byte2 = string2[pos2++];

				if (byte1 >= 65 && byte1 <= 90) {
					byte1 += 0x20;
				}
				if (byte2 >= 65 && byte2 <= 90) {
					byte2 += 0x20;
				}

				if (byte1 < byte2) {
					return -1;
				}
				if (byte1 > byte2) {
					return 1;
				}
			}
		}

		internal static int TrrntZipStringCompareCase(string string1, string string2) {
			var res = Math.Sign(TrrntZipStringCompare(string1, string2));
			return res != 0 ? res : Math.Sign(string.Compare(string1, string2, StringComparison.Ordinal));
		}

		private static void splitFilename(string filename, out string path, out string name, out string ext) {
			var dirIndex = filename.LastIndexOf('/');

			if (dirIndex >= 0) {
				path = filename.Substring(0, dirIndex);
				name = filename.Substring(dirIndex + 1);
			} else {
				path = "";
				name = filename;
			}

			var extIndex = name.LastIndexOf('.');

			if (extIndex >= 0) {
				ext = name.Substring(extIndex + 1);
				name = name.Substring(0, extIndex);
			} else {
				ext = "";
			}
		}

		public static int Trrnt7ZipStringCompare(string string1, string string2) {

			splitFilename(string1, out var path1, out var name1, out var ext1);
			splitFilename(string2, out var path2, out var name2, out var ext2);

			var res = Math.Sign(string.Compare(ext1, ext2, StringComparison.Ordinal));
			if (res != 0) {
				return res;
			}

			res = Math.Sign(string.Compare(name1, name2, StringComparison.Ordinal));
			if (res != 0) {
				return res;
			}

			res = Math.Sign(string.Compare(path1, path2, StringComparison.Ordinal));
			if (res != 0) {
				return res;
			}

			return 0;
		}
	}
}
