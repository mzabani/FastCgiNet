using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public abstract class RecordBase
	{
		protected byte[] Header;

		public byte Version {
			get
			{
				return Header[0];
			}
			private set
			{
				Header[0] = value;
			}
		}
		public virtual RecordType RecordType {
			get
			{
				return (RecordType)Header[1];
			}
			protected set
			{
				Header[1] = (byte)value;
			}
		}
		public ushort RequestId
		{
			get
			{
				return (ushort)((Header[2] << 8) + Header[3]);
			}
			protected set
			{
				Header[2] = (byte)(value >> 8);
				Header[3] = (byte)(value & byte.MaxValue);
			}
		}
		public ushort ContentLength
		{
			get
			{
				return (ushort)((Header[4] << 8) + Header[5]);
			}
			protected set
			{
				Header[4] = (byte)(value >> 8);
				Header[5] = (byte)(value & byte.MaxValue);
			}
		}
		public byte PaddingLength {
			get
			{
				return Header[6];
			}
			protected set
			{
				Header[6] = value;
			}
		}
		public byte Reserved {
			get
			{
				return Header[7];
			}
			private set
			{
				Header[7] = value;
			}
		}

		public bool IsManagementRecord
		{
			get
			{
				return RequestId == 0;
			}
		}

		public bool IsByteStreamRecord
		{
			get
			{
				return RecordType == RecordType.FCGIStdout || RecordType == RecordType.FCGIStdin || RecordType == RecordType.FCGIStderr;
			}
		}

		protected ArraySegment<byte> CalculatePaddingAndGetHeaderBytes()
		{
			//TODO: Calculate padding
			PaddingLength = 0;
			return new ArraySegment<byte>(Header);
		}

		protected IEnumerable<ArraySegment<byte>> GetPaddingBytes()
		{
			// Send padding, we can use the first byte in the header array
			//TODO: Send in blocks of up to 8 bytes (header size)
			for (int i = 0; i < PaddingLength; ++i)
			{
				yield return new ArraySegment<byte>(Header, 0, 1);
			}
		}

		/// <summary>
		/// Enumerates byte array segments that compose this record. This is useful to send through a socket, for instance.
		/// Do not modify these byte arrays as they may be the byte arrays that form the underlying stream.
		/// </summary>
		/// <remarks>The first ArraySegment enumerated is guaranteed to be the header of the record, being therefore 8 bytes long.</remarks>
		public abstract IEnumerable<ArraySegment<byte>> GetBytes();
		
		/// <summary>
		/// When more bytes that belong to this record have been received, feed them with this method, which will
		/// always do the right thing. If the newly fed bytes are enough to finish this record, than endOfRecord
		/// will be set to the offset of the last byte belonging to this record.
		/// </summary>
		internal abstract void FeedBytes(byte[] data, int offset, int length, out int endOfRecord);

		protected void AssertArrayOperation(byte[] data, int offset, int length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException("The length to be copied has to be greater than zero"); 
			else if (ByteCopyUtils.CheckArrayBounds(data, offset, length) == false)
				throw new InvalidOperationException("Can't do this operation. It would go out of the array's bounds");
		}

		public RecordBase()
		{
			Header = new byte[8];
			Version = 1;
		}

		public RecordBase(RecordType type, ushort requestId)
			: this()
		{
			RecordType = type;
			RequestId = requestId;
		}

		internal RecordBase(byte[] data, int offset, int length, RecordType recordType)
			: this()
		{
			if (length < 8 || data.Length < 8)
				throw new ArgumentException("The length available in the array must be at least 8 bytes");
			AssertArrayOperation(data, offset, length);
			
			Array.Copy(data, offset, Header, 0, 8);

			// Checks to make sure the intended recordtype is the one in the passed buffer
			if (RecordType != recordType)
				throw new InvalidOperationException("The header supplied to this record is not a valid header for this type of record");
		}
	}
}
