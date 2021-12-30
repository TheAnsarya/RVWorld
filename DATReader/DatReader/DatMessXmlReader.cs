using System.Xml;
using DATReader.DatStore;
using DATReader.Utils;

namespace DATReader.DatReader {
	public static class DatMessXmlReader {
		public static bool ReadDat(XmlDocument doc, string strFilename, out DatHeader datHeader) {
			datHeader = new DatHeader { BaseDir = new DatDir(DatFileType.UnSet) };
			if (!LoadHeaderFromDat(doc, strFilename, datHeader)) {
				return false;
			}

			var gameNodeList = doc.DocumentElement?.SelectNodes("software");

			if (gameNodeList == null) {
				return false;
			}
			for (var i = 0; i < gameNodeList.Count; i++) {
				LoadGameFromDat(datHeader.BaseDir, gameNodeList[i]);
			}

			return true;
		}

		private static bool LoadHeaderFromDat(XmlDocument doc, string filename, DatHeader datHeader) {
			var head = doc.SelectNodes("softwarelist");
			if (head == null) {
				return false;
			}

			if (head.Count == 0) {
				return false;
			}

			if (head[0].Attributes == null) {
				return false;
			}

			datHeader.Filename = filename;
			datHeader.Name = VarFix.String(head[0].Attributes.GetNamedItem("name"));
			datHeader.Description = VarFix.String(head[0].Attributes.GetNamedItem("description"));

			return true;
		}

		private static void LoadGameFromDat(DatDir parentDir, XmlNode gameNode) {
			if (gameNode.Attributes == null) {
				return;
			}

			var dDir = new DatDir(DatFileType.UnSet) {
				Name = VarFix.String(gameNode.Attributes.GetNamedItem("name")),
				DGame = new DatGame()
			};

			var dGame = dDir.DGame;
			dGame.Description = VarFix.String(gameNode.SelectSingleNode("description"));
			dGame.RomOf = VarFix.String(gameNode.Attributes?.GetNamedItem("romof"));
			dGame.CloneOf = VarFix.String(gameNode.Attributes?.GetNamedItem("cloneof"));
			dGame.Year = VarFix.String(gameNode.SelectSingleNode("year"));
			dGame.Manufacturer = VarFix.String(gameNode.SelectSingleNode("publisher"));

			var partNodeList = gameNode.SelectNodes("part");
			if (partNodeList == null) {
				return;
			}

			for (var iP = 0; iP < partNodeList.Count; iP++) {
				var indexContinue = -1;
				var dataAreaNodeList = partNodeList[iP].SelectNodes("dataarea");
				if (dataAreaNodeList == null) {
					continue;
				}
				for (var iD = 0; iD < dataAreaNodeList.Count; iD++) {
					var romNodeList = dataAreaNodeList[iD].SelectNodes("rom");
					if (romNodeList == null) {
						continue;
					}
					for (var iR = 0; iR < romNodeList.Count; iR++) {
						LoadRomFromDat(dDir, romNodeList[iR], ref indexContinue);
					}
				}
			}

			for (var iP = 0; iP < partNodeList.Count; iP++) {
				var diskAreaNodeList = partNodeList[iP].SelectNodes("diskarea");
				if (diskAreaNodeList == null) {
					continue;
				}
				for (var iD = 0; iD < diskAreaNodeList.Count; iD++) {
					var romNodeList = diskAreaNodeList[iD].SelectNodes("disk");
					if (romNodeList == null) {
						continue;
					}
					for (var iR = 0; iR < romNodeList.Count; iR++) {
						LoadDiskFromDat(dDir, romNodeList[iR]);
					}
				}
			}

			if (dDir.ChildCount > 0) {
				parentDir.ChildAdd(dDir);
			}
		}

		private static void LoadRomFromDat(DatDir parentDir, XmlNode romNode, ref int indexContinue) {
			if (romNode.Attributes == null) {
				return;
			}

			var name = romNode.Attributes.GetNamedItem("name");
			var loadflag = VarFix.String(romNode.Attributes.GetNamedItem("loadflag"));
			if (name != null) {
				var dRom = new DatFile(DatFileType.UnSet) {
					Name = VarFix.String(name),
					Size = VarFix.ULong(romNode.Attributes.GetNamedItem("size")),
					CRC = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("crc"), 8),
					SHA1 = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
					Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status"))
				};

				indexContinue = parentDir.ChildAdd(dRom);
			} else if (loadflag.ToLower() == "continue") {
				var tRom = (DatFile)parentDir.Child(indexContinue);
				tRom.Size += VarFix.ULong(romNode.Attributes.GetNamedItem("size"));
			} else if (loadflag.ToLower() == "ignore") {
				var tRom = (DatFile)parentDir.Child(indexContinue);
				tRom.Size += VarFix.ULong(romNode.Attributes.GetNamedItem("size"));
			}
		}

		private static void LoadDiskFromDat(DatDir parentDir, XmlNode romNode) {
			if (romNode.Attributes == null) {
				return;
			}

			var dRom = new DatFile(DatFileType.UnSet) {
				Name = VarFix.CleanCHD(romNode.Attributes.GetNamedItem("name")),
				SHA1 = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
				Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status")),
				isDisk = true
			};

			parentDir.ChildAdd(dRom);
		}
	}
}