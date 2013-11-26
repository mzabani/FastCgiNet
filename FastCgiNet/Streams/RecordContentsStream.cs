using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace FastCgiNet.Streams
{
	/// <summary>
	/// Use this Stream whenever you have to work with a record's contents. This stream is specially designed
	/// to be optimal for socket operations (avoids unnecessary buffering) and to check for invalid operations when it comes to FastCgi records.
	/// </summary>
	public class RecordContentsStream : Stream
	{
		//TODO: Watch out for large byte arrays, since this would promote them straight to Gen2 of the GC,
		// while they are in fact short lived objects

		private long position;

		/// <summary>
		/// The blocks of memory that have been written to this stream.
		/// </summary>
		internal LinkedList<byte[]> MemoryBlocks;
		private int length;

		/// <summary>
		/// Whether this record's contents reached its maximum size.
		/// </summary>
		public bool IsFull
		{
			get
			{
				return length == RecordBase.MaxContentLength;
			}
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			else if (buffer.Length < offset + count)
				throw new ArgumentException("The sum of offset and count is larger than the buffer length");
			else if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException("offset or count is negative");

			if (position == length)
				return 0;

			int positionSoFar = 0;
			int bytesCopied = 0;
			foreach (var arr in MemoryBlocks)
			{
				if (positionSoFar + arr.Length > position && positionSoFar < position + count)
				{
					int toArrayOffset = offset + bytesCopied;

					int startCopyingFrom = (int)position - positionSoFar;
					if (startCopyingFrom < 0)
						startCopyingFrom = 0;

					int copyLength = count - bytesCopied;
					if (copyLength + startCopyingFrom > arr.Length)
						copyLength = arr.Length - startCopyingFrom;

					Array.Copy(arr, startCopyingFrom, buffer, toArrayOffset, copyLength);
					bytesCopied += copyLength;
				}

				positionSoFar += arr.Length;
			}

			// Advances the stream
			position += bytesCopied;

			return bytesCopied;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			// Users can seek anywhere, they just can't write or read out of bounds..
			if (origin == SeekOrigin.Begin)
				position = offset;
			else if (origin == SeekOrigin.Current)
				position = position + offset;
			else if (origin == SeekOrigin.End)
				position = length + offset;

			return position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Writes to this stream, effectively copying bytes from <paramref name="buffer"/> to internal buffers.
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			//TODO: Alloc buffers respecting a fixed size, and reuse them in case we don't fill them up. Is it worth it?
			//TODO: Proper Write in positions other than the end of the stream
			if (position != length)
				throw new NotImplementedException("At the moment, only writing at the end of the stream is supported");

			var internalBuffer = new byte[count];
			Array.Copy (buffer, offset, internalBuffer, 0, count);
			MemoryBlocks.AddLast(internalBuffer);
			length += count;
			position += count;

			// Check that we didn't go over the size limit
			if (length > RecordBase.MaxContentLength)
				throw new InvalidOperationException("You can't write more than " + RecordBase.MaxContentLength + " bytes to a record's content");
		}

		public override bool CanRead {
			get
			{
				return true;
			}
		}

		public override bool CanSeek {
			get
			{
				return true;
			}
		}

		public override bool CanWrite {
			get
			{
				return true;
			}
		}

		public override long Length {
			get
			{
				return length;
			}
		}

		public override long Position {
			get
			{
				return position;
			}
			set
			{
				Seek(value, SeekOrigin.Begin);
			}
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var b = obj as RecordContentsStream;
			if (b == null)
				return false;

			// Check lenghts first
			if (this.Length != b.Length)
				return false;

			// Compare byte by byte.. kind of expensive
			byte[] bufForB = new byte[128];
			byte[] bufForA = new byte[128];
			this.Position = 0;
			b.Position = 0;

			while (b.Read(bufForB, 0, bufForB.Length) > 0)
			{
				this.Read(bufForA, 0, bufForA.Length);

				if (!ByteUtils.AreEqual(bufForA, bufForB))
					return false;
			}

			return true;
		}

		public override int GetHashCode ()
		{
			return length + 31 * MemoryBlocks.Sum(mb => mb.GetHashCode());
		}

		public RecordContentsStream ()
		{
			MemoryBlocks = new LinkedList<byte[]>();
			length = 0;
			position = 0;
		}
	}
}
