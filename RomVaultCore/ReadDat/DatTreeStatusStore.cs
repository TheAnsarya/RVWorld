using System.Collections.Generic;
using RomVaultCore.RvDB;

namespace RomVaultCore.ReadDat {
	public class DatTreeStatusStore {
		private readonly Dictionary<string, RvTreeRow> treeRows = new Dictionary<string, RvTreeRow>();

		public void PreStoreTreeValue(RvFile lDir) {
			var dbIndex = 0;
			while (dbIndex < lDir.ChildCount) {
				var dbChild = lDir.Child(dbIndex);

				if (dbChild.Tree != null) {
					var path = dbChild.TreeFullName;
					if (treeRows.ContainsKey(path)) {
						treeRows.Remove(path);
						treeRows.Add(path, null);
					} else {
						treeRows.Add(dbChild.TreeFullName, dbChild.Tree);
					}
				}
				if (dbChild?.FileType == FileType.Dir) {
					PreStoreTreeValue(dbChild);
				}

				dbIndex++;
			}
		}
		public void SetBackTreeValues(RvFile lDir) {
			var dbIndex = 0;
			while (dbIndex < lDir.ChildCount) {
				var dbChild = lDir.Child(dbIndex);

				if (dbChild.Tree != null) {
					if (treeRows.TryGetValue(dbChild.TreeFullName, out var rVal)) {
						if (rVal != null && rVal != dbChild.Tree) {
							dbChild.Tree.SetChecked(rVal.Checked, true);
							dbChild.Tree.SetTreeExpanded(rVal.TreeExpanded, true);
						}
					}
				}

				if (dbChild?.FileType == FileType.Dir) {
					SetBackTreeValues(dbChild);
				}

				dbIndex++;
			}
		}
	}
}
