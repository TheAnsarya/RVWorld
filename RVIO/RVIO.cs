﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RVIO {
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileAttributes {
		ReadOnly = 1,
		Hidden = 2,
		System = 4,
		Directory = 16,
		Archive = 32,
		Device = 64,
		Normal = 128,
		Temporary = 256,
		SparseFile = 512,
		ReparsePoint = 1024,
		Compressed = 2048,
		Offline = 4096,
		NotContentIndexed = 8192,
		Encrypted = 16384,
	}
	public static class Error {
		public static int GetLastError() => Marshal.GetLastWin32Error();
	}

	public static class Unix {
		public static bool IsUnix {
			get {
				var p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 6) || (p == 128));
			}
		}
	}

	public class FileInfo {
		public string Name;
		public string FullName;
		public long LastWriteTime;
		public long LastAccessTime;
		public long CreationTime;
		public long Length;

		public FileInfo() { }

		public FileInfo(string path) {
			FullName = path;
			Name = Path.GetFileName(path);

			var fi = new System.IO.FileInfo(NameFix.AddLongPathPrefix(path));

			if (!fi.Exists) {
				return;
			}

			Length = fi.Length;
			try { LastWriteTime = fi.LastWriteTimeUtc.Ticks; } catch { LastWriteTime = 0; }

			try { LastAccessTime = fi.LastAccessTimeUtc.Ticks; } catch { LastAccessTime = 0; }

			try { CreationTime = fi.CreationTimeUtc.Ticks; } catch { CreationTime = 0; }
		}
	}

	public class DirectoryInfo {
		public string Name;
		public string FullName;
		public long LastWriteTime;
		public long LastAccessTime;
		public long CreationTime;

		public DirectoryInfo() { }
		public DirectoryInfo(string path) {
			FullName = path;
			Name = Path.GetFileName(path);

			var fi = new System.IO.DirectoryInfo(NameFix.AddLongPathPrefix(path));

			if (!fi.Exists) {
				return;
			}

			try { LastWriteTime = fi.LastWriteTimeUtc.Ticks; } catch { LastWriteTime = 0; }

			try { LastAccessTime = fi.LastAccessTimeUtc.Ticks; } catch { LastAccessTime = 0; }

			try { CreationTime = fi.CreationTimeUtc.Ticks; } catch { CreationTime = 0; }
		}

		public DirectoryInfo[] GetDirectories() {
			var dirs = new List<DirectoryInfo>();

			try {
				var di = new System.IO.DirectoryInfo(NameFix.AddLongPathPrefix(FullName));
				if (!di.Exists) {
					return dirs.ToArray();
				}

				var arrDi = di.GetDirectories();
				foreach (var tDi in arrDi) {
					try {
						var lDi = new DirectoryInfo {
							Name = tDi.Name,
							FullName = Path.Combine(FullName, tDi.Name)
						};
						try { lDi.LastWriteTime = tDi.LastWriteTimeUtc.Ticks; } catch { lDi.LastWriteTime = 0; }

						try { lDi.LastAccessTime = tDi.LastAccessTimeUtc.Ticks; } catch { lDi.LastWriteTime = 0; }

						try { lDi.CreationTime = tDi.CreationTimeUtc.Ticks; } catch { lDi.LastWriteTime = 0; }

						dirs.Add(lDi);
					} catch { }
				}
			} catch {

			}

			return dirs.ToArray();
		}

		public FileInfo[] GetFiles(string SearchPattern = "*") {
			var files = new List<FileInfo>();

			try {
				var di = new System.IO.DirectoryInfo(NameFix.AddLongPathPrefix(FullName));
				if (!di.Exists) {
					return files.ToArray();
				}

				var arrDi = di.GetFiles(SearchPattern);
				foreach (var tDi in arrDi) {
					try {
						var lFi = new FileInfo {
							Name = tDi.Name,
							FullName = Path.Combine(FullName, tDi.Name),
							Length = tDi.Length
						};
						try { lFi.LastWriteTime = tDi.LastWriteTimeUtc.Ticks; } catch { lFi.LastWriteTime = 0; }

						try { lFi.LastAccessTime = tDi.LastAccessTimeUtc.Ticks; } catch { lFi.LastWriteTime = 0; }

						try { lFi.CreationTime = tDi.CreationTimeUtc.Ticks; } catch { lFi.LastWriteTime = 0; }

						files.Add(lFi);
					} catch { }
				}
			} catch {
			}

			return files.ToArray();
		}
	}

	public static class Directory {
		public static bool Exists(string path) => System.IO.Directory.Exists(NameFix.AddLongPathPrefix(path));

		public static void Move(string sourceDirName, string destDirName) => System.IO.Directory.Move(NameFix.AddLongPathPrefix(sourceDirName), NameFix.AddLongPathPrefix(destDirName));

		public static void Delete(string path) => System.IO.Directory.Delete(NameFix.AddLongPathPrefix(path));

		public static void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(NameFix.AddLongPathPrefix(path));
	}

	public static class File {
		public static bool Exists(string path) => System.IO.File.Exists(NameFix.AddLongPathPrefix(path));
		public static void Copy(string sourceFileName, string destfileName) => Copy(sourceFileName, destfileName, true);
		public static void Copy(string sourceFileName, string destFileName, bool overwrite) => System.IO.File.Copy(NameFix.AddLongPathPrefix(sourceFileName), NameFix.AddLongPathPrefix(destFileName), overwrite);

		public static void Move(string sourceFileName, string destFileName) => System.IO.File.Move(NameFix.AddLongPathPrefix(sourceFileName), NameFix.AddLongPathPrefix(destFileName));

		public static void Delete(string path) => System.IO.File.Delete(NameFix.AddLongPathPrefix(path));

		public static bool SetAttributes(string path, FileAttributes fileAttributes) {
			try {
				System.IO.File.SetAttributes(NameFix.AddLongPathPrefix(path), (System.IO.FileAttributes)fileAttributes);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public static StreamWriter CreateText(string filename) {
			var errorCode = FileStream.OpenFileWrite(filename, out var fStream);
			return errorCode != 0 ? null : new StreamWriter(fStream);
		}
		public static StreamReader OpenText(string filename, Encoding Enc) {
			var errorCode = FileStream.OpenFileRead(filename, out Stream fStream);
			return errorCode != 0 ? null : new StreamReader(fStream, Enc);
		}

		public static string ReadAllText(string filename) => throw new NotImplementedException();
	}

	public static class Path {
		private const char DirectorySeparatorChar = '\\';
		private const char AltDirectorySeparatorChar = '/';
		private const char VolumeSeparatorChar = ':';

		public static char DirSeparatorChar => Unix.IsUnix ? AltDirectorySeparatorChar : DirectorySeparatorChar;

		public static string GetExtension(string path) => System.IO.Path.GetExtension(path);
		public static string Combine(string path1, string path2) {
			if (Unix.IsUnix) {
				return System.IO.Path.Combine(path1, path2);
			}

			if (path1 == null || path2 == null) {
				throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
			}
			//CheckInvalidPathChars(path1);
			//CheckInvalidPathChars(path2);

			if (path2.Length == 0) {
				return path1;
			}

			if (path1.Length == 0) {
				return path2;
			}

			if (IsPathRooted(path2)) {
				return path2;
			}

			var ch = path1[path1.Length - 1];
			if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar) {
				return path1 + DirectorySeparatorChar + path2;
			}

			return path1 + path2;
		}
		private static bool IsPathRooted(string path) {
			if (path != null) {
				//CheckInvalidPathChars(path);

				var length = path.Length;
				if (
					(length >= 1 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar)) ||
					(length >= 2 && path[1] == VolumeSeparatorChar)
				) {
					return true;
				}
			}

			return false;
		}

		public static string GetFileNameWithoutExtension(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

		public static string GetFileName(string path) => System.IO.Path.GetFileName(path);
		public static string GetDirectoryName(string path) => System.IO.Path.GetDirectoryName(path);
	}

	public static class FileStream {
		public static Stream OpenFileRead(string path, out int result) {
			result = OpenFileRead(path, out Stream stream);
			return stream;
		}

		public static int OpenFileRead(string path, out Stream stream) {
			try {
				stream = new System.IO.FileStream(NameFix.AddLongPathPrefix(path), FileMode.Open, FileAccess.Read);
				return 0;
			} catch (Exception) {
				stream = null;
				return Marshal.GetLastWin32Error();
			}
		}

		public static int OpenFileWrite(string path, out Stream stream) {
			try {
				stream = new System.IO.FileStream(NameFix.AddLongPathPrefix(path), FileMode.Create, FileAccess.ReadWrite);
				return 0;
			} catch (Exception) {
				stream = null;
				return Marshal.GetLastWin32Error();
			}
		}
	}

	public static class NameFix {
		public static string GetShortPath(string path) {
			if (Unix.IsUnix) {
				return path;
			}

			int remove;
			string retPath;
			if (path.StartsWith(@"\\")) {
				retPath = @"\\?\UNC\" + path.Substring(2);
				remove = 8;
			} else {
				retPath = path;
				if (path.Substring(1, 1) != ":") {
					retPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), retPath);
				}

				retPath = CleanDots(retPath);
				retPath = @"\\?\" + retPath;
				remove = 4;
			}

			const int MAX_PATH = 300;
			var shortPath = new StringBuilder(MAX_PATH);
			Win32Native.GetShortPathName(retPath, shortPath, MAX_PATH);
			retPath = shortPath.ToString();

			retPath = retPath.Substring(remove);
			if (remove == 8) {
				retPath = "\\" + retPath;
			}

			return retPath;
		}

		internal static string AddLongPathPrefix(string path) {
			if (Unix.IsUnix) {
				return path;
			}

			if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\")) {
				return path;
			}

			if (path.StartsWith(@"\\")) {
				return @"\\?\UNC\" + path.Substring(2);
			}

			var retPath = path;
			if (path.Substring(1, 1) != ":") {
				retPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), retPath);
			}

			retPath = CleanDots(retPath);

			return @"\\?\" + retPath;
		}

		private static string CleanDots(string path) {
			var retPath = path;
			while (retPath.Contains(@"\..\")) {
				var index = retPath.IndexOf(@"\..\");
				var path1 = retPath.Substring(0, index);
				var path2 = retPath.Substring(index + 4);

				var path1Back = path1.LastIndexOf(@"\");

				retPath = path1.Substring(0, path1Back + 1) + path2;
			}

			return retPath;

		}
	}
}
