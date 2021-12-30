using System.IO;
using System.Text;

namespace RomVaultCore.RvDB {
	public class RvTreeRow {
		public enum TreeSelect {
			UnSelected,
			Selected,
			Locked
		}

		private long _filePointer = -1;
		public object UiObject;

		public RvTreeRow() {
			TreeExpanded = true;
			Checked = TreeSelect.Selected;
		}

		public bool TreeExpanded { get; private set; }

		public void SetTreeExpanded(bool value, bool CoreActive) {
			if (TreeExpanded == value) {
				return;
			}

			TreeExpanded = value;

			if (!CoreActive) {
				CacheUpdate();
			}
		}

		public TreeSelect Checked { get; private set; }

		public void SetChecked(TreeSelect value, bool CoreActive) {
			if (Checked == value) {
				return;
			}

			Checked = value;
			if (!CoreActive) {
				CacheUpdate();
			}
		}

		public void Write(BinaryWriter bw) {
			_filePointer = bw.BaseStream.Position;
			bw.Write(TreeExpanded);
			bw.Write((byte)Checked);
		}

		public void Read(BinaryReader br) {
			_filePointer = br.BaseStream.Position;
			TreeExpanded = br.ReadBoolean();
			Checked = (TreeSelect)br.ReadByte();
		}

		private static FileStream fsl;
		private static BinaryWriter bwl;

		public static void OpenStream() {
			if (!RVIO.File.Exists(Settings.rvSettings.CacheFile)) {
				fsl = null;
				bwl = null;
				return;
			}
			fsl = new FileStream(Settings.rvSettings.CacheFile, FileMode.Open, FileAccess.Write);
			bwl = new BinaryWriter(fsl, Encoding.UTF8, true);
		}

		public static void CloseStream() {
			bwl?.Flush();
			bwl?.Close();
			bwl?.Dispose();
			fsl?.Close();
			fsl?.Dispose();
			bwl = null;
			fsl = null;
		}

		private void CacheUpdate() {
			if (_filePointer < 0) {
				return;
			}

			if (fsl != null && bwl != null) {
				fsl.Position = _filePointer;
				bwl.Write(TreeExpanded);
				bwl.Write((byte)Checked);
				return;
			}

			using (var fs = new FileStream(Settings.rvSettings.CacheFile, FileMode.Open, FileAccess.Write)) {
				using (var bw = new BinaryWriter(fs, Encoding.UTF8, true)) {
					fs.Position = _filePointer;
					bw.Write(TreeExpanded);
					bw.Write((byte)Checked);

					bw.Flush();
					bw.Close();
				}

				fs.Close();
			}
		}
	}
}
