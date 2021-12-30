using System;
using System.IO;
using Compress.Support.Compression.RangeCoder;

namespace Compress.Support.Compression.PPmd {
	public class PpmdStream : Stream {
		private readonly PpmdProperties properties;
		private readonly Stream stream;
		private readonly bool compress;
		private readonly I1.Model model;
		private readonly H.ModelPPM modelH;
		private readonly Decoder decoder;
		private long position = 0;

		public PpmdStream(PpmdProperties properties, Stream stream, bool compress) {
			this.properties = properties;
			this.stream = stream;
			this.compress = compress;

			if (properties.Version == PpmdVersion.I1) {
				model = new I1.Model();
				if (compress) {
					model.EncodeStart(properties);
				} else {
					model.DecodeStart(stream, properties);
				}
			}
			if (properties.Version == PpmdVersion.H) {
				modelH = new H.ModelPPM();
				if (compress) {
					throw new NotImplementedException();
				} else {
					modelH.decodeInit(stream, properties.ModelOrder, properties.AllocatorSize);
				}
			}
			if (properties.Version == PpmdVersion.H7z) {
				modelH = new H.ModelPPM();
				if (compress) {
					throw new NotImplementedException();
				} else {
					modelH.decodeInit(null, properties.ModelOrder, properties.AllocatorSize);
				}

				decoder = new Decoder();
				decoder.Init(stream);
			}
		}

		public override bool CanRead => !compress;

		public override bool CanSeek => false;

		public override bool CanWrite => compress;

		public override void Flush() {
		}

		protected override void Dispose(bool isDisposing) {
			if (isDisposing) {
				if (compress) {
					model.EncodeBlock(stream, new MemoryStream(), true);
				}
			}
			base.Dispose(isDisposing);
		}

		public override long Length => throw new NotImplementedException();

		public override long Position {
			get => position;
			set => throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (compress) {
				return 0;
			}

			var size = 0;
			if (properties.Version == PpmdVersion.I1) {
				size = model.DecodeBlock(stream, buffer, offset, count);
			}

			if (properties.Version == PpmdVersion.H) {
				int c;
				while (size < count && (c = modelH.decodeChar()) >= 0) {
					buffer[offset++] = (byte)c;
					size++;
				}
			}
			if (properties.Version == PpmdVersion.H7z) {
				int c;
				while (size < count && (c = modelH.decodeChar(decoder)) >= 0) {
					buffer[offset++] = (byte)c;
					size++;
				}
			}
			position += size;
			return size;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if (origin != SeekOrigin.Current) {
				throw new NotImplementedException();
			}

			var tmpBuff = new byte[1024];
			var sizeToGo = offset;
			while (sizeToGo > 0) {
				var sizenow = sizeToGo > 1024 ? 1024 : (int)sizeToGo;
				Read(tmpBuff, 0, sizenow);
				sizeToGo -= sizenow;
			}

			return offset;
		}

		public override void SetLength(long value) => throw new NotImplementedException();

		public override void Write(byte[] buffer, int offset, int count) {
			if (compress) {
				model.EncodeBlock(stream, new MemoryStream(buffer, offset, count), false);
			}
		}
	}
}
