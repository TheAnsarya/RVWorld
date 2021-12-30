namespace Compress.Support.Compression.RangeCoder {
	internal struct BitEncoder {
		public const int KNumBitModelTotalBits = 11;
		public const uint KBitModelTotal = 1 << KNumBitModelTotalBits;
		private const int KNumMoveBits = 5;
		private const int KNumMoveReducingBits = 2;
		public const int KNumBitPriceShiftBits = 6;
		private uint Prob;

		public void Init() => Prob = KBitModelTotal >> 1;

		public void UpdateModel(uint symbol) {
			if (symbol == 0) {
				Prob += (KBitModelTotal - Prob) >> KNumMoveBits;
			} else {
				Prob -= (Prob) >> KNumMoveBits;
			}
		}

		public void Encode(Encoder encoder, uint symbol) {
			// encoder.EncodeBit(Prob, kNumBitModelTotalBits, symbol);
			// UpdateModel(symbol);
			var newBound = (encoder.Range >> KNumBitModelTotalBits) * Prob;
			if (symbol == 0) {
				encoder.Range = newBound;
				Prob += (KBitModelTotal - Prob) >> KNumMoveBits;
			} else {
				encoder.Low += newBound;
				encoder.Range -= newBound;
				Prob -= (Prob) >> KNumMoveBits;
			}

			if (encoder.Range < Encoder.KTopValue) {
				encoder.Range <<= 8;
				encoder.ShiftLow();
			}
		}

		private static readonly uint[] ProbPrices = new uint[KBitModelTotal >> KNumMoveReducingBits];

		static BitEncoder() {
			const int kNumBits = (KNumBitModelTotalBits - KNumMoveReducingBits);
			for (var i = kNumBits - 1; i >= 0; i--) {
				var start = (uint)1 << (kNumBits - i - 1);
				var end = (uint)1 << (kNumBits - i);
				for (var j = start; j < end; j++) {
					ProbPrices[j] = ((uint)i << KNumBitPriceShiftBits) +
						(((end - j) << KNumBitPriceShiftBits) >> (kNumBits - i - 1));
				}
			}
		}

		public uint GetPrice(uint symbol) => ProbPrices[(((Prob - symbol) ^ ((-(int)symbol))) & (KBitModelTotal - 1)) >> KNumMoveReducingBits];
		public uint GetPrice0() => ProbPrices[Prob >> KNumMoveReducingBits];
		public uint GetPrice1() => ProbPrices[(KBitModelTotal - Prob) >> KNumMoveReducingBits];
	}

	internal struct BitDecoder {
		public const int KNumBitModelTotalBits = 11;
		public const uint KBitModelTotal = (1 << KNumBitModelTotalBits);
		private const int KNumMoveBits = 5;
		private uint Prob;

		public void UpdateModel(int numMoveBits, uint symbol) {
			if (symbol == 0) {
				Prob += (KBitModelTotal - Prob) >> numMoveBits;
			} else {
				Prob -= (Prob) >> numMoveBits;
			}
		}

		public void Init() => Prob = KBitModelTotal >> 1;

		public uint Decode(RangeCoder.Decoder rangeDecoder) {
			var newBound = (rangeDecoder.Range >> KNumBitModelTotalBits) * Prob;
			if (rangeDecoder.Code < newBound) {
				rangeDecoder.Range = newBound;
				Prob += (KBitModelTotal - Prob) >> KNumMoveBits;
				if (rangeDecoder.Range < Decoder.KTopValue) {
					rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
					rangeDecoder.Total++;

				}


				return 0;
			} else {
				rangeDecoder.Range -= newBound;
				rangeDecoder.Code -= newBound;
				Prob -= (Prob) >> KNumMoveBits;
				if (rangeDecoder.Range < Decoder.KTopValue) {
					rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
					rangeDecoder.Total++;
				}


				return 1;
			}
		}
	}
}
