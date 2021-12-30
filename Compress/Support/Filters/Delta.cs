﻿using System;
using System.IO;

namespace Compress.Support.Filters {
	public class Delta : Stream {
		private readonly Stream _baseStream;
		private long _position;
		private readonly byte[] _bVal;
		private readonly int _dSize;
		private int _bIndex;

		// properties values are 0,1,3
		public Delta(byte[] properties, Stream inputStream) {
			_dSize = properties[0] + 1;
			_bVal = new byte[_dSize];

			_baseStream = inputStream;
		}

		public override void Flush() => throw new NotImplementedException();

		public override long Seek(long offset, SeekOrigin origin) {
			if (origin != SeekOrigin.Current) {
				throw new NotImplementedException();
			}

			const int bufferSize = 10240;
			var seekBuffer = new byte[bufferSize];
			var seekToGo = offset;
			while (seekToGo > 0) {
				var get = seekToGo > bufferSize ? bufferSize : seekToGo;
				Read(seekBuffer, 0, (int)get);
				seekToGo -= get;
			}
			return _position;
		}

		public override void SetLength(long value) => throw new NotImplementedException();

		public override int Read(byte[] buffer, int offset, int count) {
			var read = _baseStream.Read(buffer, offset, count);

			for (var i = 0; i < read; i++) {
				buffer[i] = _bVal[_bIndex] = (byte)(buffer[i] + _bVal[_bIndex]);
				_bIndex = (_bIndex + 1) % _dSize;
			}

			_position += read;

			return read;
		}

		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;
		public override long Length => _baseStream.Length;
		public override long Position {
			get => _position;
			set => throw new NotImplementedException();
		}
	}
}
