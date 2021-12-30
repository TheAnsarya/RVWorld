using Compress.SevenZip;
using Compress.ZipFile;
using Directory = RVIO.Directory;
using FileStream = RVIO.FileStream;
using Path = RVIO.Path;

namespace Compress.Support.Utils {
	public delegate void MessageBack(string message);

	public class ArchiveExtract {
		public MessageBack MessageCallBack = null;

		public ArchiveExtract() { }
		public ArchiveExtract(MessageBack messageCallBack) => MessageCallBack = messageCallBack;

		public bool FullExtract(string filename, string outDir) {
			MessageCallBack?.Invoke($"Processing file: {filename}");
			if (!string.IsNullOrEmpty(outDir)) {
				MessageCallBack?.Invoke($"Output dir: {outDir}");
			}

			var ext = Path.GetExtension(filename);

			ICompress z = null;
			switch (ext.ToLower()) {
				case ".zip":
					z = new Zip();
					break;
				case ".7z":
					z = new SevenZ();
					break;

			}

			if (z == null) {
				MessageCallBack?.Invoke($"Unknown file type {ext}");
				return false;
			}

			var zRet = z.ZipFileOpen(filename);
			if (zRet != ZipReturn.ZipGood) {
				MessageCallBack?.Invoke($"Error opening archive {zRet}");
				return false;
			}

			ulong buflen = 409600;
			var buffer = new byte[buflen];

			for (var i = 0; i < z.LocalFilesCount(); i++) {
				var lf = z.GetLocalFile(i);
				byte[] cread = null;
				var filenameOut = lf.Filename;
				if (lf.IsDirectory) {
					var outFullDir = Path.Combine(outDir, filenameOut.Substring(0, filenameOut.Length - 1).Replace('/', '\\'));
					Directory.CreateDirectory(outFullDir);
					continue;
				} else {
					MessageCallBack?.Invoke($"Extracting {filenameOut}");
					var fOut = Path.Combine(outDir, filenameOut.Replace('/', '\\'));
					var dOut = Path.GetDirectoryName(fOut);
					if (!string.IsNullOrWhiteSpace(dOut) && !Directory.Exists(dOut)) {
						Directory.CreateDirectory(dOut);
					}

					var errorCode = FileStream.OpenFileWrite(fOut, out var sWrite);
					if (errorCode != 0) {
						MessageCallBack?.Invoke($"Error opening outputfile {fOut}");
					}

					z.ZipFileOpenReadStream(i, out var sRead, out _);

					CRC crc = new();
					var sizeToGo = lf.UncompressedSize;

					while (sizeToGo > 0) {
						var sizeNow = sizeToGo > buflen ? buflen : sizeToGo;
						var sizeRead = sRead.Read(buffer, 0, (int)sizeNow);

						crc.SlurpBlock(buffer, 0, sizeRead);
						sWrite.Write(buffer, 0, sizeRead);
						sizeToGo -= (ulong)sizeRead;
					}

					sWrite.Close();
					sWrite.Dispose();

					cread = crc.Crc32ResultB;
				}

				var fread = lf.CRC;
				if (cread[0] != fread[0] || cread[1] != fread[1] || cread[2] != fread[2] || cread[3] != fread[3]) {
					MessageCallBack?.Invoke($"CRC error. Expected {fread.ToHex()} found {cread.ToHex()}");
					return false;
				}
			}
			return true;
		}
	}
}
