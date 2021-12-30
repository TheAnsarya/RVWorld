
namespace Compress.Support.Utils {
	public static class Reporter {
		public static string ToArrayString(this ulong[] arr) {
			if (arr == null) {
				return "NULL";
			}

			var ret = $"({arr.Length}) " + arr[0].ToString();
			for (var i = 1; i < arr.Length; i++) {
				ret += "," + arr[i].ToString();
			}

			return ret;
		}
		public static string ToArrayString(this byte[] arr) {
			if (arr == null) {
				return "NULL";
			}

			var ret = $"({arr.Length}) " + arr[0].ToString("X2");
			for (var i = 1; i < arr.Length; i++) {
				ret += "," + arr[i].ToString("X2");
			}

			return ret;
		}

		public static string ToHex(this byte[] arr) {
			if (arr == null) {
				return "NULL";
			}

			var ret = "";
			for (var i = 0; i < arr.Length; i++) {
				ret += arr[i].ToString("X2");
			}

			return ret;
		}


		public static string ToHex(this uint? v) => v == null ? "NULL" : ((uint)v).ToString("X8");
		public static string ToHex(this ulong? v) => v == null ? "NULL" : ((ulong)v).ToString("X8");
	}
}
