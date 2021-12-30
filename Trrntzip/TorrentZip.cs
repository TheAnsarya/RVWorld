using System.Collections.Generic;
using System.Diagnostics;
using Compress;
using Compress.SevenZip;
using Compress.ZipFile;
using RVIO;

namespace TrrntZip {
	public delegate void StatusCallback(int threadId, int precent);

	public delegate void LogCallback(int threadId, string log);

	public class TorrentZip {
		private readonly byte[] _buffer;
		public StatusCallback StatusCallBack;
		public LogCallback StatusLogCallBack;
		public int ThreadId;

		public TorrentZip() => _buffer = new byte[1024 * 1024];

		public TrrntZipStatus Process(FileInfo fi, PauseCancel pc = null) {
			if (Program.VerboseLogging) {
				StatusLogCallBack?.Invoke(ThreadId, "");
			}

			StatusLogCallBack?.Invoke(ThreadId, fi.Name + " - ");

			// First open the zip (7z) file, and fail out if it is corrupt.
			var tzs = OpenZip(fi, out var zipFile);
			// this will return ValidTrrntZip or CorruptZip.

			for (var i = 0; i < zipFile.LocalFilesCount(); i++) {
				var lf = zipFile.GetLocalFile(i);
				Debug.WriteLine("Name = " + lf.Filename + " , " + lf.UncompressedSize);
			}

			if ((tzs & TrrntZipStatus.CorruptZip) == TrrntZipStatus.CorruptZip) {
				StatusLogCallBack?.Invoke(ThreadId, "Zip file is corrupt");
				return TrrntZipStatus.CorruptZip;
			}

			// the zip file may have found a valid trrntzip header, but we now check that all the file info
			// is actually valid, and may invalidate it being a valid trrntzip if any problem is found.

			var zippedFiles = ReadZipContent(zipFile);

			var localOutputType = Program.OutZip;

			// check if the compression type has changed
			zipType inputType;
			switch (zipFile) {
				case Zip _:
					tzs |= TorrentZipCheck.CheckZipFiles(ref zippedFiles, ThreadId, StatusLogCallBack);
					inputType = zipType.zip;
					break;
				case SevenZ _:
					tzs |= TorrentZipCheck.CheckSevenZipFiles(ref zippedFiles, ThreadId, StatusLogCallBack);
					inputType = zipType.sevenzip;
					break;
				case Compress.File.File _:
					inputType = zipType.file;
					if (localOutputType == zipType.archive) {
						localOutputType = zipType.zip;
					}

					break;
				default:
					return TrrntZipStatus.Unknown;
			}

			var outputType = localOutputType == zipType.archive ? inputType : Program.OutZip;

			var compressionChanged = inputType != outputType;

			// if tza is now just 'ValidTrrntzip' the it is fully valid, and nothing needs to be done to it.

			if (((tzs == TrrntZipStatus.ValidTrrntzip) && !compressionChanged && !Program.ForceReZip) || Program.CheckOnly) {
				StatusLogCallBack?.Invoke(ThreadId, "Skipping File");
				zipFile.ZipFileClose();
				return tzs;
			}

			// if compressionChanged then the required file order will also have changed to need to re-sort the files.
			if (compressionChanged) {
				switch (outputType) {
					case zipType.zip:
						tzs |= TorrentZipCheck.CheckZipFiles(ref zippedFiles, ThreadId, StatusLogCallBack);
						break;
					case zipType.sevenzip:
						tzs |= TorrentZipCheck.CheckSevenZipFiles(ref zippedFiles, ThreadId, StatusLogCallBack);
						break;
				}
			}

			StatusLogCallBack?.Invoke(ThreadId, "TorrentZipping");
			var fixedTzs = TorrentZipRebuild.ReZipFiles(zippedFiles, zipFile, _buffer, StatusCallBack, StatusLogCallBack, ThreadId, pc);
			return fixedTzs;
		}

		private TrrntZipStatus OpenZip(FileInfo fi, out ICompress zipFile) {
			var ext = Path.GetExtension(fi.Name);
			switch (ext) {
				case ".7z":
					zipFile = new SevenZ();
					break;
				case ".zip":
					zipFile = new Zip();
					break;
				default:
					zipFile = new Compress.File.File();
					break;
			}

			var zr = zipFile.ZipFileOpen(fi.FullName, fi.LastWriteTime);
			if (zr != ZipReturn.ZipGood) {
				return TrrntZipStatus.CorruptZip;
			}

			var tzStatus = TrrntZipStatus.Unknown;

			// first check if the file is a trrntip files
			if (zipFile.ZipStatus == ZipStatus.TrrntZip) {
				tzStatus |= TrrntZipStatus.ValidTrrntzip;
			}

			return tzStatus;
		}

		private static List<ZippedFile> ReadZipContent(ICompress zipFile) {
			var zippedFiles = new List<ZippedFile>();
			for (var i = 0; i < zipFile.LocalFilesCount(); i++) {
				var lf = zipFile.GetLocalFile(i);
				zippedFiles.Add(
					new ZippedFile {
						Index = i,
						Name = lf.Filename,
						ByteCRC = lf.CRC,
						Size = lf.UncompressedSize
					}
				);

			}

			return zippedFiles;
		}
	}
}
