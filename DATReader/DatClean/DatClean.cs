using System;
using DATReader.DatStore;

namespace DATReader.DatClean {
	public enum RemoveSubType {
		KeepAllSubDirs,
		RemoveAllSubDirs,
		RemoveAllIfNoConflicts,
		RemoveSubIfSingleFiles,
		RemoveSubIfNameMatches // <-- invalid and removed
	}

	public static partial class DatClean {

		public static void DirectoryExpand(DatDir dDir) {
			var arrDir = dDir.ToArray();
			var foundSubDir = false;
			foreach (var db in arrDir) {
				if (CheckDir(db)) {
					if (db.Name.Contains("\\")) {
						foundSubDir = true;
						break;
					}
				}
			}

			if (foundSubDir) {
				dDir.ChildrenClear();
				foreach (var db in arrDir) {
					if (CheckDir(db)) {
						if (db.Name.Contains("\\")) {
							var dirName = db.Name;
							var split = dirName.IndexOf("\\", StringComparison.Ordinal);
							var part0 = dirName.Substring(0, split);
							var part1 = dirName.Substring(split + 1);

							db.Name = part1;
							var dirFind = new DatDir(DatFileType.Dir) { Name = part0 };
							if (dDir.ChildNameSearch(dirFind, out var index) != 0) {
								dDir.ChildAdd(dirFind);
							} else {
								dirFind = (DatDir)dDir.Child(index);
							}

							if (part1.Length > 0) {
								dirFind.ChildAdd(db);
							}

							continue;
						}
					}
					dDir.ChildAdd(db);
				}

				arrDir = dDir.ToArray();
			}

			foreach (var db in arrDir) {
				if (db is DatDir dbDir) {
					DirectoryExpand(dbDir);
				}
			}
		}

		public static void RemoveDeviceRef(DatDir dDir) {
			var arrDir = dDir.ToArray();
			if (arrDir == null) {
				return;
			}

			foreach (var db in arrDir) {
				if (db is DatDir ddir) {
					if (ddir.DGame != null) {
						ddir.DGame.device_ref = null;
					}

					RemoveDeviceRef(ddir);
				}
			}
		}

		private static bool CheckDir(DatBase db) {
			var dft = db.DatFileType;

			switch (dft) {
				// files inside of zips/7zips do not need to be expanded
				case DatFileType.File7Zip:
				case DatFileType.FileTorrentZip:
					return false;
				// everything else should be fully expanded
				default:
					return true;
			}
		}

		/*
        public static void SetCompressionMethod(DatFileType ft, bool CompressionOverrideDAT, bool FilesOnly, DatHeader dh)
        {
            if (!CompressionOverrideDAT)
            {
                switch (dh.Compression?.ToLower())
                {
                    case "unzip":
                    case "file":
                        ft = DatFileType.Dir;
                        break;
                    case "7zip":
                    case "7z":
                        ft = DatFileType.Dir7Zip;
                        break;
                    case "zip":
                        ft = DatFileType.DirTorrentZip;
                        break;
                }
            }

            if (FilesOnly)
                ft = DatFileType.Dir;

            switch (ft)
            {
                case DatFileType.Dir:
                    DatSetCompressionType.SetFile(dh.BaseDir);
                    return;
                case DatFileType.Dir7Zip:
                    DatSetCompressionType.SetZip(dh.BaseDir, true);
                    return;
                default:
                    DatSetCompressionType.SetZip(dh.BaseDir);
                    return;
            }
        }
        */

	}
}
