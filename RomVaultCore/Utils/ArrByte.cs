using System;
using System.IO;

namespace RomVaultCore.Utils {
	public static class ArrByte {
		public static void WriteByteArray(this BinaryWriter bw, byte[] b) {
			bw.Write((byte)b.Length);
			bw.Write(b);
		}


		public static byte[] ReadByteArray(this BinaryReader br) {
			var len = br.ReadByte();
			return br.ReadBytes(len);
		}

		public static byte[] Copy(this byte[] b) {
			if (b == null) {
				return null;
			}
			var retB = new byte[b.Length];
			Array.Copy(b, 0, retB, 0, b.Length);
			return retB;
		}

		public static byte[] Copy(this byte[] b, int index, int count) {
			if (b == null) {
				return null;
			}
			var retB = new byte[count];
			Array.Copy(b, index, retB, 0, count);
			return retB;
		}


		public static bool BCompare(byte[] b1, byte[] b2) {
			if (b1 == null || b2 == null) {
				return false;
			}

			if (b1.Length != b2.Length) {
				return false;
			}

			for (var i = 0; i < b1.Length; i++) {
				if (b1[i] != b2[i]) {
					return false;
				}
			}

			return true;
		}

		public static bool ECompare(byte[] b1, byte[] b2) {
			if (b1 == null || b2 == null) {
				return true;
			}

			if (b1.Length != b2.Length) {
				return false;
			}

			for (var i = 0; i < b1.Length; i++) {
				if (b1[i] != b2[i]) {
					return false;
				}
			}

			return true;
		}


		public static int ICompare(byte[] b1, byte[] b2) {
			var b1Len = b1?.Length ?? 0;
			var b2Len = b2?.Length ?? 0;

			var p = 0;
			for (; ; )
			{
				if (b1Len == p) {
					return b2Len == p ? 0 : -1;
				}

				if (b2Len == p) {
					return 1;
				}

				if (b1[p] < b2[p]) {
					return -1;
				}

				if (b1[p] > b2[p]) {
					return 1;
				}

				p++;
			}
		}

		//https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa#24343727
		private static readonly uint[] Lookup32 = CreateLookup32();

		private static uint[] CreateLookup32() {
			var result = new uint[256];
			for (var i = 0; i < 256; i++) {
				var s = i.ToString("X2").ToLower();
				result[i] = s[0] + ((uint)s[1] << 16);
			}

			return result;
		}
		private static string ByteArrayToHexViaLookup32(byte[] bytes) {
			var result = new char[bytes.Length * 2];
			var c = 0;
			foreach (var b in bytes) {
				var val = Lookup32[b];
				result[c++] = (char)val;
				result[c++] = (char)(val >> 16);
			}

			return new string(result);
		}

		public static string ToHexString(this byte[] b) => b == null ? "" : ByteArrayToHexViaLookup32(b);
	}
}
