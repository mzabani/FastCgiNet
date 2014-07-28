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
        /// The position of the first byte of this stream in the stream returned by the supplied implementation of <see cref="ISecondaryStorageOps"/>.
        /// </summary>
        private long secondaryStoragePosition;
        private ISecondaryStorageOps secondaryStorageOps;
		/// <summary>
		/// If not stored in secondary storage, this list represents all the the data that has been written to this stream.
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

            // 1. If we are reading from secondary storage, it is quite straight forward
            if (secondaryStorageOps != null)
            {
                var dataStream = secondaryStorageOps.ReadData();
                dataStream.Seek(secondaryStoragePosition + position, SeekOrigin.Begin);
                return dataStream.Read(buffer, offset, count);
            }

            // 2. If we are reading from memory, then it is not so simple
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
		/// Writes to this stream, effectively copying bytes from <paramref name="buffer"/> to internal buffers or to secondary storage accordingly.
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			//TODO: Proper Write in positions other than the end of the stream
			if (position != length)
				throw new NotImplementedException("At the moment, only writing at the end of the stream is supported");

            // 1. Secondary storage
            if (secondaryStorageOps != null)
            {
                var dataStream = secondaryStorageOps.ReadData();
                dataStream.Write(buffer, offset, count);
                return;
            }
            else
            {
                // 2. In memory storage
                var internalBuffer = new byte[count];
                Array.Copy(buffer, offset, internalBuffer, 0, count);
                MemoryBlocks.AddLast(internalBuffer);
            }

			length += count;
			position += count;

			// Check that we didn't go over the size limit
			if (length > RecordBase.MaxContentLength)
				throw new InvalidOperationException("You can't write more than " + RecordBase.MaxContentLength + " bytes to a record's contents");
		}

		public override bool CanRead
        {
			get
			{
				return true;
			}
		}

		public override bool CanSeek
        {
			get
			{
				return true;
			}
		}

		public override bool CanWrite
        {
			get
			{
				return true;
			}
		}

		public override long Length
        {
			get
			{
				return length;
			}
		}

		public override long Position
        {
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

			// Check lengths first
			if (this.Length != b.Length)
				return false;

			// Compare byte by byte.. kind of expensive
            //TODO: Do we really need such a strict equality criterium?
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

		public override int GetHashCode()
		{
            // We are very lax here.. no need to read from secondary storage or go over every single byte
            // just to hash this stream
			return length + 31 * MemoryBlocks.Take(1).Sum(mb => mb.GetHashCode());
		}

        /// <summary>
        /// Creates a stream that stores one record's contents in memory.
        /// </summary>
		public RecordContentsStream()
		{
			MemoryBlocks = new LinkedList<byte[]>();
            secondaryStorageOps = null;
			length = 0;
			position = 0;
		}

        /// <summary>
        /// Creates a stream that stores one record's contents in secondary storage. The life-cycle of the supplied
        /// <see cref="ISecondaryStorageOps"/> has no relation with this stream whatsoever.
        /// </summary>
        public RecordContentsStream(ISecondaryStorageOps secondaryStorageOps)
        {
            if (secondaryStorageOps == null)
                throw new ArgumentNullException("secondaryStorageOps");

            this.secondaryStorageOps = secondaryStorageOps;
            secondaryStoragePosition = secondaryStorageOps.ReadData().Position;
            length = 0;
            position = 0;
        }
	}
}
