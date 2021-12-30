﻿/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2020                                 *
 ******************************************************/

using System;

namespace RomVaultCore.Utils {
	public static class VarFix {
		public static byte[] CleanMD5SHA1(string checksum, int length) {
			if (string.IsNullOrEmpty(checksum)) {
				return null;
			}

			checksum = checksum.ToLower().Trim();

			if (checksum.Length >= 2) {
				if (checksum.Substring(0, 2) == "0x") {
					checksum = checksum.Substring(2);
				}
			}

			if (string.IsNullOrEmpty(checksum)) {
				return null;
			}

			if (checksum == "-") {
				return null;
			}

			//if (checksum.Length % 2 == 1)
			//    checksum = "0" + checksum;

			//if (checksum.Length != length)
			//    return null;

			while (checksum.Length < length) {
				checksum = "0" + checksum;
			}

			var retL = checksum.Length / 2;
			var retB = new byte[retL];

			for (var i = 0; i < retL; i++) {
				retB[i] = Convert.ToByte(checksum.Substring(i * 2, 2), 16);
			}

			return retB;
		}
	}
}
