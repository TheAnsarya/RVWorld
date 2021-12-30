using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Compress.SevenZip.Structure;
using Compress.Support.Compression.BZip2;
using Compress.Support.Compression.LZMA;
using Compress.Support.Compression.PPmd;
using Compress.Support.Compression.zStd;
using Compress.Support.Filters;
using FileStream = RVIO.FileStream;

namespace Compress.SevenZip {
	public partial class SevenZ {
		private int _streamIndex = -1;
		private Stream _stream;

		public ZipReturn ZipFileOpenReadStream(int index, out Stream stream, out ulong unCompressedSize) {
			Debug.WriteLine("Opening File " + _localFiles[index].Filename);
			stream = null;
			unCompressedSize = 0;

			try {
				if (ZipOpen != ZipOpenType.OpenRead) {
					return ZipReturn.ZipErrorGettingDataStream;
				}

				if (GetLocalFile(index).IsDirectory) {
					return ZipReturn.ZipTryingToAccessADirectory;
				}

				unCompressedSize = _localFiles[index].UncompressedSize;
				var thisStreamIndex = _localFiles[index].StreamIndex;
				var streamOffset = _localFiles[index].StreamOffset;

				if ((thisStreamIndex == _streamIndex) && (streamOffset >= (ulong)_stream.Position)) {
					stream = _stream;
					stream.Seek((long)_localFiles[index].StreamOffset - _stream.Position, SeekOrigin.Current);
					return ZipReturn.ZipGood;
				}

				ZipFileCloseReadStream();
				_streamIndex = thisStreamIndex;

				if (_header.StreamsInfo == null) {
					stream = null;
					return ZipReturn.ZipGood;
				}

				var folder = _header.StreamsInfo.Folders[_streamIndex];

				// first make the List of Decompressors streams
				var codersNeeded = folder.Coders.Length;

				List<InStreamSourceInfo> allInputStreams = new();
				for (var i = 0; i < codersNeeded; i++) {
					folder.Coders[i].DecoderStream = null;
					allInputStreams.AddRange(folder.Coders[i].InputStreamsSourceInfo);
				}

				// now use the binding pairs to links the outputs to the inputs
				var bindPairsCount = folder.BindPairs.Length;
				for (var i = 0; i < bindPairsCount; i++) {
					allInputStreams[(int)folder.BindPairs[i].InIndex].InStreamSource = InStreamSource.CompStreamOutput;
					allInputStreams[(int)folder.BindPairs[i].InIndex].InStreamIndex = folder.BindPairs[i].OutIndex;
					folder.Coders[(int)folder.BindPairs[i].OutIndex].OutputUsedInternally = true;
				}

				// next use the stream indises to connect the remaining input streams from the sourcefile
				var packedStreamsCount = folder.PackedStreamIndices.Length;
				for (var i = 0; i < packedStreamsCount; i++) {
					var packedStreamIndex = (ulong)i + folder.PackedStreamIndexBase;

					// create and open the source file stream if needed
					if (_header.StreamsInfo.PackedStreams[packedStreamIndex].PackedStream == null) {
						_header.StreamsInfo.PackedStreams[packedStreamIndex].PackedStream = CloneStream(_zipFs);
					}

					_header.StreamsInfo.PackedStreams[packedStreamIndex].PackedStream.Seek(
						_baseOffset + (long)_header.StreamsInfo.PackedStreams[packedStreamIndex].StreamPosition, SeekOrigin.Begin);


					allInputStreams[(int)folder.PackedStreamIndices[i]].InStreamSource = InStreamSource.FileStream;
					allInputStreams[(int)folder.PackedStreamIndices[i]].InStreamIndex = packedStreamIndex;
				}

				List<Stream> inputCoders = new();

				var allCodersComplete = false;
				while (!allCodersComplete) {
					allCodersComplete = true;
					for (var i = 0; i < codersNeeded; i++) {
						var coder = folder.Coders[i];

						// check is decoder already processed
						if (coder.DecoderStream != null) {
							continue;
						}

						inputCoders.Clear();
						for (var j = 0; j < (int)coder.NumInStreams; j++) {
							if (coder.InputStreamsSourceInfo[j].InStreamSource == InStreamSource.FileStream) {
								inputCoders.Add(_header.StreamsInfo.PackedStreams[coder.InputStreamsSourceInfo[j].InStreamIndex].PackedStream);
							} else if (coder.InputStreamsSourceInfo[j].InStreamSource == InStreamSource.CompStreamOutput) {
								if (folder.Coders[coder.InputStreamsSourceInfo[j].InStreamIndex].DecoderStream == null) {
									break;
								}

								inputCoders.Add(folder.Coders[coder.InputStreamsSourceInfo[j].InStreamIndex].DecoderStream);
							} else {
								// unknown input type so error
								return ZipReturn.ZipDecodeError;
							}
						}

						if (inputCoders.Count == (int)coder.NumInStreams) {
							// all inputs streams are available to make the decoder stream
							switch (coder.DecoderType) {
								case DecompressType.Stored:
									coder.DecoderStream = inputCoders[0];
									break;
								case DecompressType.Delta:
									coder.DecoderStream = new Delta(folder.Coders[i].Properties, inputCoders[0]);
									break;
								case DecompressType.LZMA:
									coder.DecoderStream = new LzmaStream(folder.Coders[i].Properties, inputCoders[0]);
									break;
								case DecompressType.LZMA2:
									coder.DecoderStream = new LzmaStream(folder.Coders[i].Properties, inputCoders[0]);
									break;
								case DecompressType.PPMd:
									coder.DecoderStream = new PpmdStream(new PpmdProperties(folder.Coders[i].Properties), inputCoders[0], false);
									break;
								case DecompressType.BZip2:
									coder.DecoderStream = new CBZip2InputStream(inputCoders[0], false);
									break;
								case DecompressType.BCJ:
									coder.DecoderStream = new BCJFilter(false, inputCoders[0]);
									break;
								case DecompressType.BCJ2:
									coder.DecoderStream = new BCJ2Filter(inputCoders[0], inputCoders[1], inputCoders[2], inputCoders[3]);
									break;
								case DecompressType.ZSTD:
									coder.DecoderStream = new zStd(inputCoders[0]);
									break;
								default:
									return ZipReturn.ZipDecodeError;
							}
						}

						// if skipped a coder need to loop round again
						if (coder.DecoderStream == null) {
							allCodersComplete = false;
						}
					}
				}
				// find the final output stream and return it.
				var outputStream = -1;
				for (var i = 0; i < codersNeeded; i++) {
					var coder = folder.Coders[i];
					if (!coder.OutputUsedInternally) {
						outputStream = i;
					}
				}

				stream = folder.Coders[outputStream].DecoderStream;
				stream.Seek((long)_localFiles[index].StreamOffset, SeekOrigin.Current);

				_stream = stream;

				return ZipReturn.ZipGood;

			} catch (Exception) {
				return ZipReturn.ZipErrorGettingDataStream;
			}
		}

		private Stream CloneStream(Stream s) {
			switch (s) {
				case System.IO.FileStream _:
					var errorCode = FileStream.OpenFileRead(ZipFilename, out Stream streamOut);
					return errorCode != 0 ? null : streamOut;

				case MemoryStream memStream:
					var pos = memStream.Position;
					memStream.Position = 0;
					var newStream = new byte[memStream.Length];
					memStream.Read(newStream, 0, (int)memStream.Length);
					MemoryStream ret = new(newStream, false);
					memStream.Position = pos;
					return ret;
			}

			return null;
		}

		public ZipReturn ZipFileCloseReadStream() {
			if (_streamIndex != -1) {
				if (_header.StreamsInfo != null) {
					var folder = _header.StreamsInfo.Folders[_streamIndex];

					foreach (var c in folder.Coders) {
						var ds = c?.DecoderStream;
						if (ds == null) {
							continue;
						}

						ds.Close();
						ds.Dispose();
						c.DecoderStream = null;
					}
				}
			}

			_streamIndex = -1;

			if (_header?.StreamsInfo != null) {
				foreach (var psi in _header.StreamsInfo.PackedStreams) {
					if (psi?.PackedStream == null) {
						continue;
					}

					psi.PackedStream.Close();
					psi.PackedStream.Dispose();
					psi.PackedStream = null;
				}
			}

			return ZipReturn.ZipGood;
		}
	}
}
