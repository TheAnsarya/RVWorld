using System.Collections.Generic;
using DATReader.DatStore;
using DATReader.Utils;

namespace DATReader.DatClean {
	public static partial class DatClean {
		public static void DatSetMakeNonMergeSet(DatDir tDat) {
			for (var g = 0; g < tDat.ChildCount; g++) {
				var mGame = (DatDir)tDat.Child(g);

				if (mGame.DGame == null) {
					DatSetMakeNonMergeSet(mGame);
				} else {

					var dGame = mGame.DGame;

					if (dGame?.device_ref == null) {
						continue;
					}

					var devices = new List<DatDir> { mGame };

					foreach (var device in dGame.device_ref) {
						AddDevice(device, devices, tDat);
					}
					devices.RemoveAt(0);

					foreach (var device in devices) {
						for (var i = 0; i < device.ChildCount; i++) {
							var df0 = (DatFile)device.Child(i);
							var crcFound = false;
							for (var j = 0; j < mGame.ChildCount; j++) {
								var df1 = (DatFile)mGame.Child(j);
								if (ArrByte.bCompare(df0.SHA1, df1.SHA1) && df0.Name == df1.Name) {
									crcFound = true;
									break;
								}
							}
							if (!crcFound) {
								mGame.ChildAdd(device.Child(i));
							}
						}
					}
				}
			}
		}

		private static void AddDevice(string device, List<DatDir> devices, DatDir tDat) {
			if (tDat.ChildNameSearch(new DatDir(tDat.DatFileType) { Name = device }, out var index) != 0) {
				return;
			}

			var devChild = (DatDir)tDat.Child(index);
			if (devChild == null) {
				return;
			}

			if (devices.Contains(devChild)) {
				return;
			}

			devices.Add(devChild);

			var childDev = devChild.DGame?.device_ref;
			if (childDev == null) {
				return;
			}

			foreach (var deviceChild in childDev) {
				AddDevice(deviceChild, devices, tDat);
			}
		}

		public static void RemoveDevices(DatDir tDat) {
			var children = tDat.ToArray();

			tDat.ChildrenClear();
			foreach (var child in children) {
				var mGame = (DatDir)child;

				if (mGame.DGame == null) {
					RemoveDevices(mGame);
					tDat.ChildAdd(mGame);
				} else {
					var dGame = mGame.DGame;

					if (dGame != null && dGame.IsDevice == "yes" && dGame.Runnable == "no") {
						continue;
					}

					tDat.ChildAdd(mGame);
				}
			}
		}
	}
}
