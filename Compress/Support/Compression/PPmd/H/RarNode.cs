using System.Text;

namespace Compress.Support.Compression.PPmd.H {
	internal class RarNode : Pointer {
		private int next; //rarnode pointer

		public const int Size = 4;

		public RarNode(byte[] Memory)
			: base(Memory) {
		}

		internal int GetNext() {
			if (Memory != null) {
				next = Utility.readIntLittleEndian(Memory, Address);
			}

			return next;
		}

		internal void SetNext(RarNode next) => SetNext(next.Address);

		internal void SetNext(int next) {
			this.next = next;
			if (Memory != null) {
				Utility.WriteLittleEndian(Memory, Address, next);
			}
		}

		public override string ToString() {
			var buffer = new StringBuilder();
			buffer.Append("State[");
			buffer.Append("\n  Address=");
			buffer.Append(Address);
			buffer.Append("\n  size=");
			buffer.Append(Size);
			buffer.Append("\n  next=");
			buffer.Append(GetNext());
			buffer.Append("\n]");
			return buffer.ToString();
		}
	}
}
