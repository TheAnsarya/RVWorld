using System.Collections.Generic;
using DATReader.DatStore;
using DATReader.Utils;

namespace DATReader.DatClean {
	public static partial class DatClean {
		public static void DatSetMakeMergeSet(DatDir tDat, bool mergeWithGameName = true) {
			// look for merged roms, check if a rom exists in a parent set where the Name,Size and CRC all match.

			for (var g = 0; g < tDat.ChildCount; g++) {
				var mGame = (DatDir)tDat.ChildSorted(g);

				if (mGame.DGame == null) {
					DatSetMakeMergeSet(mGame, mergeWithGameName);
					continue;
				}

				// find all parents of this game
				var lstParentGames = new List<DatDir>();
				DatFindParentSets.FindParentSet(mGame, tDat, true, ref lstParentGames);

				// if no parents are found then just set all children as kept
				if (lstParentGames.Count == 0) {
					for (var r = 0; r < mGame.ChildCount; r++) {
						if (mGame.Child(r) is DatFile dfGame) {
							RomCheckCollect(dfGame, false);
						}
					}

					continue;
				}

				var pGames = new List<DatDir>();
				var pBios = new List<DatDir>();
				foreach (var dd in lstParentGames) {
					if (dd.DGame.IsBios?.ToLower() == "yes") {
						pBios.Add(dd);
					} else {
						pGames.Add(dd);
					}
				}

				var mGameTest = mGame.ToArray();
				var mGameKeep = new List<DatBase>();

				foreach (var tGame in mGameTest) {
					if (((DatFile)tGame).Status == "nodump") {
						mGame.ChildAdd(tGame);
						continue;
					}

					// first remove any file that is in a parent BIOS set
					var found = false;
					foreach (var romofGame in pBios) {
						for (var r1 = 0; r1 < romofGame.ChildCount; r1++) {
							// size/checksum compare, so name does not need to match
							// if (!string.Equals(mGame[r].Name, romofGame.Child(r1).Name, StringComparison.OrdinalIgnoreCase))
							// {
							//     continue;
							// }

							var size0 = ((DatFile)tGame).Size;
							var size1 = ((DatFile)romofGame.Child(r1)).Size;
							if ((size0 != null) && (size1 != null) && (size0 != size1)) {
								continue;
							}

							var crc0 = ((DatFile)tGame).CRC;
							var crc1 = ((DatFile)romofGame.Child(r1)).CRC;
							if ((crc0 != null) && (crc1 != null) && !ArrByte.bCompare(crc0, crc1)) {
								continue;
							}

							var sha0 = ((DatFile)tGame).SHA1;
							var sha1 = ((DatFile)romofGame.Child(r1)).SHA1;
							if ((sha0 != null) && (sha1 != null) && !ArrByte.bCompare(sha0, sha1)) {
								continue;
							}

							var md50 = ((DatFile)tGame).MD5;
							var md51 = ((DatFile)romofGame.Child(r1)).MD5;
							if ((md50 != null) && (md51 != null) && !ArrByte.bCompare(md50, md51)) {
								continue;
							}

							if (((DatFile)tGame).isDisk != ((DatFile)romofGame.Child(r1)).isDisk) {
								continue;
							}

							// not needed as we are now checking for nodumps at the top of this code
							// don't merge if only one of the ROM is nodump
							//if (((DatFile)romofGame.Child(r1)).Status == "nodump" != (((DatFile)mGame[r]).Status == "nodump"))
							//{
							//    continue;
							//}

							found = true;
							break;
						}

						if (found) {
							break;
						}
					}

					if (!found) {
						mGameKeep.Add(tGame);
					}
				}

				mGame.ChildrenClear();

				if (pGames.Count == 0) {
					foreach (var tGame in mGameKeep) {
						mGame.ChildAdd(tGame);
					}

					continue;
				}

				var romOfTopParent = pGames[pGames.Count - 1];

				foreach (var tGame in mGameKeep) {
					if (mergeWithGameName && !((DatFile)tGame).isDisk) {
						tGame.Name = mGame.Name + "\\" + tGame.Name;
					}

					romOfTopParent.ChildAdd(tGame);
				}
			}
		}
	}
}
