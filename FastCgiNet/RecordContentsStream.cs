using System;
using System.IO;
using System.Collections.Generic;

namespace FastCgiNet
{
	/// <summary>
	/// Use this Stream whenever you have to work with a record's contents. This stream is specially designed
	/// to be optimal for socket operations (avoids unnecessary buffering) and to check for invalid operations when it comes to FastCgi records.
	/// </summary>
	public class RecordContentsStream : Stream
	{
		//TODO: Watch out for large byte arrays, since this would promote them straight to Gen2 of the GC,
		// while they are in fact short lived objects

		const int maxLength = 65535;

		/// <summary>
		/// The blocks of memory that have been written to this stream.
		/// </summary>
		public LinkedList<byte[]> MemoryBlocks;
		int length;

		/// <summary>
		/// Whether this record's contents reached its maximum size.
		/// </summary>
		public bool IsFull
		{
			get
			{
				return length == maxLength;
			}
		}

		public override void Flush ()
		{
			//length = 0;
			//MemoryBlocks = new LinkedList<byte[]>();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int lengthSoFar = 0;
			int bytesCopied = 0;
			foreach (var arr in MemoryBlocks)
			{
				if (lengthSoFar + arr.Length > offset && lengthSoFar < offset + count)
				{
					int arrayOffset = offset - lengthSoFar;
					int copyLength = lengthSoFar + arr.Length - offset;
					if (copyLength > count)
						copyLength = count;

					Array.Copy(arr, arrayOffset, buffer, bytesCopied, copyLength);
					bytesCopied += copyLength;
				}

				lengthSoFar += buffer.Length;
			}

			return bytesCopied;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			//TODO: Correctly implement the methods in this stream now that it is public
			throw new NotImplementedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Writes to this stream, effectively copying bytes from <paramref name="buffer"/> to internal buffers.
		/// </summary>
		public override void Write (byte[] buffer, int offset, int count)
		{
			//TODO: Alloc buffers respecting a fixed size, and reuse them in case we don't fill them up. Is it worth it?

			var internalBuffer = new byte[count];
			Array.Copy (buffer, offset, internalBuffer, 0, count);
			MemoryBlocks.AddLast(internalBuffer);
			length += count;

			// Check that we didn't go over the size limit
			if (length > maxLength)
				throw new InvalidOperationException("You can't write more than " + maxLength + " bytes to a record's content");
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				return length;
			}
		}

		public override long Position {
			get {
				return length;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public RecordContentsStream ()
		{
			MemoryBlocks = new LinkedList<byte[]>();
			length = 0;
		}
	}
}

