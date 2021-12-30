using DATReader.DatStore;

namespace DATReader.Utils {
	public static class DatHasRomOf {
		public static bool HasRomOf(DatDir tDat) {
			for (var g = 0; g < tDat.ChildCount; g++) {
				if (!(tDat.Child(g) is DatDir mGame)) {
					continue;
				}

				if (mGame.DGame == null) {
					var res = HasRomOf(mGame);
					if (res) {
						return true;
					}
				} else {
					if (!string.IsNullOrWhiteSpace(mGame.DGame.RomOf)) {
						return true;
					}

					if (!string.IsNullOrWhiteSpace(mGame.DGame.CloneOf)) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
