using System.Collections.Concurrent;
using System.Collections.Generic;
using RVIO;
using TrrntZip;

namespace TrrntZipUI {
	public delegate void UpdateFileCount(int fileCount);

	public class FileAdder {
		private readonly string[] _file;
		private readonly UpdateFileCount _updateFileCount;
		private readonly BlockingCollection<cFile> _fileCollection;
		private readonly ProcessFileEndCallback _processFileEndCallBack;

		private int fileCount;

		public FileAdder(BlockingCollection<cFile> fileCollectionIn, string[] file, UpdateFileCount updateFileCount, ProcessFileEndCallback ProcessFileEndCallBack) {
			_fileCollection = fileCollectionIn;
			_file = file;
			_updateFileCount = updateFileCount;
			_processFileEndCallBack = ProcessFileEndCallBack;
		}

		public void ProcFiles() {
			fileCount = 0;

			var lstFile = new List<string>();
			foreach (var t in _file) {
				if (File.Exists(t)) {
					AddFile(t, ref lstFile);
				}
			}

			foreach (var file in lstFile) {
				var cf = new cFile() { fileId = fileCount++, filename = file };
				_fileCollection.Add(cf);
			}

			_updateFileCount?.Invoke(fileCount);

			foreach (var t in _file) {
				if (Directory.Exists(t)) {
					AddDirectory(t);
				}
			}

			_processFileEndCallBack?.Invoke(-1, 0, TrrntZipStatus.Unknown);
		}

		private void AddFile(string filename, ref List<string> cf) {
			var extn = Path.GetExtension(filename);
			extn = extn.ToLower();

			if (extn == ".zip") {
				if (TrrntZip.Program.InZip == zipType.zip || TrrntZip.Program.InZip == zipType.archive || TrrntZip.Program.InZip == zipType.all) {
					cf.Add(filename);
					return;
				}
			}

			if (extn == ".7z") {
				if (TrrntZip.Program.InZip == zipType.sevenzip || TrrntZip.Program.InZip == zipType.archive || TrrntZip.Program.InZip == zipType.all) {
					cf.Add(filename);
					return;
				}
			}

			if (TrrntZip.Program.InZip == zipType.file || TrrntZip.Program.InZip == zipType.all) {
				cf.Add(filename);
				return;
			}

			return;
		}

		private void AddDirectory(string directory) {
			var di = new DirectoryInfo(directory);

			var lstFile = new List<string>();
			var fi = di.GetFiles();
			foreach (var t in fi) {
				AddFile(t.FullName, ref lstFile);
			}

			foreach (var file in lstFile) {
				var cf = new cFile() { fileId = fileCount++, filename = file };
				_fileCollection.Add(cf);
			}

			_updateFileCount?.Invoke(fileCount);

			var diChild = di.GetDirectories();
			foreach (var t in diChild) {
				AddDirectory(t.FullName);
			}
		}
	}
}
