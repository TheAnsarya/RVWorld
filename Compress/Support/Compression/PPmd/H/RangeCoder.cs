﻿using System.IO;
using System.Text;

namespace Compress.Support.Compression.PPmd.H {
	internal class RangeCoder {
		internal const int TOP = 1 << 24;
		internal const int BOT = 1 << 15;
		internal const long UintMask = 0xFFFFffffL;

		// uint low, code, range;
		private long low, code, range;
		private readonly Stream stream;

		internal RangeCoder(Stream stream) {
			this.stream = stream;
			Init();
		}

		private void Init() {
			SubRange = new SubRange();

			low = code = 0L;
			range = 0xFFFFffffL;
			for (var i = 0; i < 4; i++) {
				code = ((code << 8) | Char) & UintMask;
			}
		}

		internal int CurrentCount {
			get {
				range = (range / SubRange.Scale) & UintMask;
				return (int)((code - low) / (range));
			}
		}

		private long Char {
			get {
				if (stream != null) {
					return stream.ReadByte();
				}

				return -1;
			}
		}

		internal SubRange SubRange {
			get;
			private set;
		}

		internal long GetCurrentShiftCount(int SHIFT) {
			range = Utility.URShift(range, SHIFT);
			return ((code - low) / (range)) & UintMask;
		}

		internal void Decode() {
			low = (low + (range * SubRange.LowCount)) & UintMask;
			range = (range * (SubRange.HighCount - SubRange.LowCount)) & UintMask;
		}

		internal void AriDecNormalize() {
			//		while ((low ^ (low + range)) < TOP || range < BOT && ((range = -low & (BOT - 1)) != 0 ? true : true)) 
			//		{
			//			code = ((code << 8) | unpackRead.getChar()&0xff)&uintMask;
			//			range = (range << 8)&uintMask;
			//			low = (low << 8)&uintMask;
			//		}

			// Rewrote for clarity
			var c2 = false;
			while ((low ^ (low + range)) < TOP || (c2 = range < BOT)) {
				if (c2) {
					range = (-low & (BOT - 1)) & UintMask;
					c2 = false;
				}

				code = ((code << 8) | Char) & UintMask;
				range = (range << 8) & UintMask;
				low = (low << 8) & UintMask;
			}
		}

		// Debug
		public override string ToString() {
			var buffer = new StringBuilder();
			buffer.Append("RangeCoder[");
			buffer.Append("\n  low=");
			buffer.Append(low);
			buffer.Append("\n  code=");
			buffer.Append(code);
			buffer.Append("\n  range=");
			buffer.Append(range);
			buffer.Append("\n  subrange=");
			buffer.Append(SubRange);
			buffer.Append("]");
			return buffer.ToString();
		}
	}

	internal class SubRange {
		// uint LowCount, HighCount, scale;
		private long lowCount, highCount, scale;

		internal void incScale(int dScale) => Scale += dScale;

		internal long HighCount {
			get => highCount;

			set => highCount = value & RangeCoder.UintMask;

		}

		internal long LowCount {
			get => lowCount & RangeCoder.UintMask;

			set => lowCount = value & RangeCoder.UintMask;

		}

		internal long Scale {
			get => scale;

			set => scale = value & RangeCoder.UintMask;

		}

		// Debug
		public override string ToString() {
			var buffer = new StringBuilder();
			buffer.Append("SubRange[");
			buffer.Append("\n  lowCount=");
			buffer.Append(lowCount);
			buffer.Append("\n  highCount=");
			buffer.Append(highCount);
			buffer.Append("\n  scale=");
			buffer.Append(scale);
			buffer.Append("]");
			return buffer.ToString();
		}
	}
}
