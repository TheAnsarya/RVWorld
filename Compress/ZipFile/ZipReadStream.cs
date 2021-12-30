using System.IO;

namespace Compress.ZipFile {
	public partial class Zip {
		public ZipReturn ZipFileOpenReadStream(int index, out Stream stream, out ulong streamSize) => ZipFileOpenReadStream(index, false, out stream, out streamSize, out var _);

		public ZipReturn ZipFileOpenReadStream(int index, bool raw, out Stream stream, out ulong streamSize, out ushort compressionMethod) {
			ZipFileCloseReadStream();

			streamSize = 0;
			compressionMethod = 0;
			stream = null;
			if (ZipOpen != ZipOpenType.OpenRead) {
				return ZipReturn.ZipReadingFromOutputFile;
			}

			var zRet = _localFiles[index].LocalFileHeaderRead(_zipFs);
			if (zRet != ZipReturn.ZipGood) {
				ZipFileClose();
				return zRet;
			}

			zRet = _localFiles[index].LocalFileOpenReadStream(_zipFs, raw, out stream, out streamSize, out compressionMethod);
			_compressionStream = stream;
			return zRet;
		}

		public ZipReturn ZipFileOpenReadStreamQuick(ulong pos, bool raw, out Stream stream, out ulong streamSize, out ushort compressionMethod) {
			ZipFileCloseReadStream();

			var tmpFile = new ZipLocalFile { RelativeOffsetOfLocalHeader = pos };
			_localFiles.Clear();
			_localFiles.Add(tmpFile);
			var zRet = tmpFile.LocalFileHeaderReadQuick(_zipFs);
			if (zRet != ZipReturn.ZipGood) {
				stream = null;
				streamSize = 0;
				compressionMethod = 0;
				return zRet;
			}

			zRet = tmpFile.LocalFileOpenReadStream(_zipFs, raw, out stream, out streamSize, out compressionMethod);
			_compressionStream = stream;
			return zRet;
		}


		public ZipReturn ZipFileCloseReadStream() {
			if (_compressionStream == null) {
				return ZipReturn.ZipGood;
			}

			if (_compressionStream != _zipFs) {
				_compressionStream.Close();
				_compressionStream.Dispose();
			}

			_compressionStream = null;

			return ZipReturn.ZipGood;
		}


	}
}
