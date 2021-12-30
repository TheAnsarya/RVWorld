using DATReader.DatStore;

namespace DATReader.DatClean {
	public static partial class DatClean {
		public static void MakeDatSingleLevel(DatHeader tDatHeader, bool useDescription, RemoveSubType subDirType, bool isFiles) {
			// KeepAllSubDirs, just does what it says
			// RemoveAllSubDirs, just does what it says
			// RemoveAllIfNoConflicts, does the conflict precheck and if a conflict is found switches to KeepAllSubDirs
			// RemoveSubsIfSingleFile, remove the subdir if there is only one file in the subdir

			var db = tDatHeader.BaseDir.ToArray();
			tDatHeader.Dir = "noautodir";

			var rootDirName = "";
			if (string.IsNullOrEmpty(rootDirName) && useDescription && !string.IsNullOrWhiteSpace(tDatHeader.Description)) {
				rootDirName = tDatHeader.Description;
			}

			if (string.IsNullOrEmpty(rootDirName)) {
				rootDirName = tDatHeader.Name;
			}

			// do a pre check to see if removing all the sub-dirs will give any name conflicts
			if (subDirType == RemoveSubType.RemoveAllIfNoConflicts) {
				var foundRepeatFilename = false;
				var rootTest = new DatDir(DatFileType.UnSet) {
					Name = rootDirName,
				};
				foreach (var set in db) {
					if (!(set is DatDir romSet)) {
						continue;
					}

					var dbr = romSet.ToArray();
					foreach (var rom in dbr) {
						var f = rootTest.ChildNameSearch(rom, out var _);
						if (f == 0) {
							foundRepeatFilename = true;
							subDirType = RemoveSubType.KeepAllSubDirs;
							break;
						}
						rootTest.ChildAdd(rom);
					}

					if (foundRepeatFilename) {
						break;
					}
				}
			}

			tDatHeader.BaseDir.ChildrenClear();

			DatDir root;
			if (isFiles) {
				root = tDatHeader.BaseDir;
			} else {
				root = new DatDir(DatFileType.UnSet) {
					Name = rootDirName,
					DGame = new DatGame { Description = tDatHeader.Description }
				};
				tDatHeader.BaseDir.ChildAdd(root);
			}

			foreach (var set in db) {
				var dirName = set.Name;
				if (!(set is DatDir romSet)) {
					continue;
				}

				var dbr = romSet.ToArray();
				foreach (var rom in dbr) {
					if (subDirType == RemoveSubType.RemoveSubIfSingleFiles) {
						if (dbr.Length != 1) {
							rom.Name = dirName + "\\" + rom.Name;
						}
					} else if (subDirType == RemoveSubType.KeepAllSubDirs) {
						rom.Name = dirName + "\\" + rom.Name;
					}
					root.ChildAdd(rom);
				}
			}
		}
	}
}
