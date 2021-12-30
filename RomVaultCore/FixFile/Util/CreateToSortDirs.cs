using System.Collections.Generic;
using RomVaultCore.RvDB;
using RVIO;

namespace RomVaultCore.FixFile.Util {
	public static partial class FixFileUtils {
		public static ReturnCode CreateToSortDirs(RvFile inFile, out RvFile outDir, out string filename) {
			var dirTree = new List<RvFile>();

			var rFile = inFile;
			while (rFile.Parent != null) {
				rFile = rFile.Parent;
				dirTree.Insert(0, rFile);
			}
			dirTree.RemoveAt(0);

			var toSort = DB.RvFileToSort();
			if (!Directory.Exists(toSort.FullName)) {
				try {
					Directory.CreateDirectory(toSort.FullName);
				} catch {
					outDir = null;
					filename = null;
					return ReturnCode.ToSortNotFound;
				}
			}
			dirTree[0] = toSort;
			var dirTreeCount = dirTree.Count;

			for (var i = 1; i < dirTreeCount; i++) {
				var baseDir = dirTree[i - 1];
				var thisDir = dirTree[i];

				var tDir = new RvFile(FileType.Dir) {
					Name = thisDir.Name,
					DatStatus = DatStatus.InToSort
				};
				var found = baseDir.ChildNameSearch(tDir, out var index);
				if (found == 0) {
					tDir = baseDir.Child(index);
				} else {
					baseDir.ChildAdd(tDir, index);
				}

				var fullpath = tDir.FullName;
				if (!Directory.Exists(fullpath)) {
					Directory.CreateDirectory(fullpath);
					var di = new DirectoryInfo(fullpath);
					tDir.SetStatus(DatStatus.InToSort, GotStatus.Got);
					tDir.FileModTimeStamp = di.LastWriteTime;
				}

				dirTree[i] = tDir;
			}

			outDir = dirTree[dirTreeCount - 1];
			filename = inFile.Name;

			var toSortFullName = Path.Combine(outDir.FullName, filename);
			var fileC = 0;
			string name = null, ext = null;

			while (File.Exists(toSortFullName)) {
				if (name == null) {
					var pIndex = inFile.Name.LastIndexOf('.');
					if (pIndex >= 0) {
						name = inFile.Name.Substring(0, pIndex);
						ext = inFile.Name.Substring(pIndex + 1);
					} else {
						name = inFile.Name;
						ext = "";
					}
				}

				filename = name + "_" + fileC + "." + ext;
				toSortFullName = Path.Combine(outDir.FullName, filename);
				fileC += 1;
			}

			return ReturnCode.Good;
		}
	}
}
