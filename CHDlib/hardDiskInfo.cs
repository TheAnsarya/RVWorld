using System;
using System.IO;

namespace CHDlib {
	internal class hard_disk_info {
		public uint length;       /* length of header data */
		public uint version;      /* drive format version */

		public uint flags;        /* flags field */
		public uint compression;  /* compression type */
		public uint[] compressions;  /* compression type array*/
		public uint totalblocks;  /* total # of blocks represented */

		public byte[] md5;          /* MD5 checksum for this drive */
		public byte[] parentmd5;    /* MD5 checksum for parent drive */
		public byte[] sha1;         /* SHA1 checksum for this drive */
		public byte[] parentsha1;   /* SHA1 checksum for parent drive */
		public byte[] rawsha1;
		public ulong mapoffset;
		public ulong metaoffset;

		public uint blocksize;     /* number of bytes per hunk */
		public uint unitbytes;

		public ulong totalbytes;




		public Stream file;

		public mapentry[] map;
	}
	[Flags]
	internal enum mapFlags {
		MAP_ENTRY_FLAG_TYPE_MASK = 0x000f,      /* what type of hunk */
		MAP_ENTRY_FLAG_NO_CRC = 0x0010,         /* no CRC is present */

		MAP_ENTRY_TYPE_INVALID = 0x0000,        /* invalid type */
		MAP_ENTRY_TYPE_COMPRESSED = 0x0001,     /* standard compression */
		MAP_ENTRY_TYPE_UNCOMPRESSED = 0x0002,   /* uncompressed data */
		MAP_ENTRY_TYPE_MINI = 0x0003,           /* mini: use offset as raw data */
		MAP_ENTRY_TYPE_SELF_HUNK = 0x0004,      /* same as another hunk in this file */
		MAP_ENTRY_TYPE_PARENT_HUNK = 0x0005     /* same as a hunk in the parent file */
	}


	internal class mapentry {
		public ulong offset;
		public uint crc;
		public ulong length;
		public mapFlags flags;

		public int UseCount;
		public byte[] BlockCache = null;
	}

}
