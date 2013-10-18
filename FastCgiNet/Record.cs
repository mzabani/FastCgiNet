using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class Record : IDisposable
	{
		byte[] header;

		public byte Version {
			get
			{
				return header[0];
			}
			private set
			{
				header[0] = value;
			}
		}
		public RecordType RecordType {
			get
			{
				return (RecordType)header[1];
			}
			private set
			{
				header[1] = (byte)value;
			}
		}
		public ushort RequestId
		{
			get
			{
				return (ushort)((header[2] << 8) + header[3]);
			}
			private set
			{
				header[2] = (byte)(value >> 8);
				header[3] = (byte)(value & byte.MaxValue);
			}
		}
		public ushort ContentLength
		{
			get
			{
				return (ushort)((header[4] << 8) + header[5]);
			}
			private set
			{
				header[4] = (byte)(value >> 8);
				header[5] = (byte)(value & byte.MaxValue);
			}
		}
		public byte PaddingLength {
			get
			{
				return header[6];
			}
			set
			{
				header[6] = value;
			}
		}
		public byte Reserved {
			get
			{
				return header[7];
			}
			private set
			{
				header[7] = value;
			}
		}

		public bool IsManagementRecord
		{
			get
			{
				return RequestId == 0;
			}
		}

		/// <summary>
		/// Whether this record contains any content at all. Empty Content records are used to end communication from one
		/// side of the request.
		/// </summary>
		public bool EmptyContentData
		{
			get
			{
				//TODO: This only makes sense for sdin, stdout and stderr records?
				return ContentLength == 0;
			}
		}

		/// <summary>
		/// If this record is one with pairs of names and values, this will be not null.
		/// </summary>
		public NameValuePairCollection NamesAndValues { get; private set; }

		/// <summary>
		/// If this is a FCGIBeginRequest, this property will be set.
		/// </summary>
		public FCGIBeginRequest BeginRequest { get; private set; }

		/// <summary>
		/// If this is a FCGIEndRequest, this property will be set.
		/// </summary>
		public FCGIEndRequest EndRequest { get; private set; }

		/// <summary>
		/// If this is a byte stream record, this property will contain its contents.
		/// </summary>
		internal ByteStreamContent ByteContent { get; private set; }

		public bool IsByteStreamRecord
		{
			get
			{
				return RecordType == RecordType.FCGIStdout || RecordType == RecordType.FCGIStdin || RecordType == RecordType.FCGIStderr;
			}
		}

		/// <summary>
		/// Use this method to get the stream that enables you to define what content will be in this record. You can also set
		/// this to a stream with the content in it. If you want to set this to a Stream, do prefer using <see cref="RecordContentsStream"/> 
		/// as it is tailor made for socket operations (avoids unnecessary buffering) and checks for record size limits. Use a different type of Stream only if you really must.
		/// </summary>
		public Stream ContentStream
		{
			get
			{
				if (!IsByteStreamRecord)
					throw new InvalidOperationException("This record's type has to be one of Stdout, Stdin or Stderr");

				if (ByteContent == null)
					ByteContent = new ByteStreamContent();

				return ByteContent.ContentFed ?? ByteContent.Content;
			}
			set
			{
				if (value is RecordContentsStream)
					ByteContent = new ByteStreamContent((RecordContentsStream) value);
				else
					ByteContent = new ByteStreamContent(value);
			}
		}

		/// <summary>
		/// Enumerates byte array segments that compose this record. This is useful to send through a socket, for instance.
		/// Do not modify these byte arrays as they may be the byte arrays that form the underlying stream.
		/// </summary>
		public IEnumerable<ArraySegment<byte>> GetBytes() {
			//TODO: Calculate padding to align structure size
			// Send the header, the contents and the padding
			if (IsByteStreamRecord)
				ContentLength = (ushort)ContentStream.Length;
			else
				ContentLength = (ushort) 0;

			PaddingLength = 0;

			yield return new ArraySegment<byte>(header);
			
			// If we know the stream is a RecordContentsStream, then we can avoid further buffering, otherwise
			// it is kind of inevitable that we may be copying buffers more than once
			if (IsByteStreamRecord)
			{
				if (ByteContent.ContentFed != null)
				{
					foreach (var buf in ByteContent.ContentFed.MemoryBlocks)
					{
						yield return new ArraySegment<byte>(buf);
					}
				}
				else
				{
					//TODO: Watch for Gen2 promotion of this short lived buffer
					byte[] buf = new byte[8192];
					Stream bodyStream = ContentStream;
					while (bodyStream.Read(buf, 0, 8192) > 0)
					{
						yield return new ArraySegment<byte>(buf);
					}
				}
			}

			// If it is a EndRequest record
			if (EndRequest != null)
			{
				byte[] endRequestBodyBytes = EndRequest.GetBytes();
				yield return new ArraySegment<byte>(endRequestBodyBytes, 0, endRequestBodyBytes.Length);
			}
			
			// Send padding, we can use the first byte in the header array
			for (int i = 0; i < PaddingLength; ++i)
			{
				yield return new ArraySegment<byte>(header, 0, 1);
			}
		}
	

		public void Dispose() {
			if (ByteContent != null)
				ByteContent.Dispose();
		}

		/// <summary>
		/// When more bytes that belong to this record have been received, feed them with this method, which will
		/// always do the right thing. If the newly fed bytes are enough to finish this record, than endOfRecord
		/// will be set to the offset of the last byte belonging to this record.
		/// </summary>
		internal void FeedBytes(byte[] data, int offset, int length, out int endOfRecord)
		{
			if (RecordType == RecordType.FCGIBeginRequest)
			{
				BeginRequest.FeedBytes(data, offset, length, out endOfRecord);
			}
			else if (RecordType == RecordType.FCGIEndRequest)
			{
				EndRequest.FeedBytes(data, offset, length, out endOfRecord);
			}
			else if (RecordType == RecordType.FCGIStdin)
			{
				// Check for empty stdin record
				if (ContentLength + PaddingLength == 0)
					throw new InvalidOperationException("This record is already ended");
				else
					ByteContent.FeedBytes(data, offset, length, out endOfRecord);
			}
			else if (RecordType == RecordType.FCGIParams)
			{
				// Watch for empty content
				if (ContentLength + PaddingLength == 0)
					throw new InvalidOperationException("This record is already ended");
				else
					NamesAndValues.FeedBytes(data, offset, length, out endOfRecord);
			}
			else
			{
				throw new NotImplementedException("");
				//TODO: Other types of records
			}
		}

		#region Constructors
		private Record()
		{
			header = new byte[8];
		}

		/// <summary>
		/// Initializes a new FastCgi record. You should not rely on this record's ContentLength, PaddingLength and EmptyContentData properties until
		/// you call <see cref="GetBytes()"/>, which will calculate and set them for you.
		/// </summary>
		/// <param name="type">The type of record to be created.</param>
		/// <param name="requestId">The RequestId of this record.</param>
		public Record(RecordType type, ushort requestId)
			: this()
		{
			Version = 1;
			RecordType = type;
			RequestId = requestId;

			if (IsByteStreamRecord)
				ByteContent = new ByteStreamContent();
		}

		internal Record (byte[] data, int offset, int length, out int endOfRecord)
			: this()
		{
			if (length < 8 || data.Length < 8)
				throw new ArgumentOutOfRangeException("The length available in the array must be at least 8 bytes");
			else if (ByteCopyUtils.CheckArrayBounds(data, offset, length) == false)
				throw new InvalidOperationException("The array is not big enough to do this");

			Array.Copy(data, offset, header, 0, 8);

			if (RecordType == RecordType.FCGIBeginRequest)
			{
				BeginRequest = new FCGIBeginRequest();
			}
			else if (RecordType == RecordType.FCGIEndRequest)
			{
				EndRequest = new FCGIEndRequest();
			}
			else if (RecordType == RecordType.FCGIStdin)
			{
				ByteContent = new ByteStreamContent(ContentLength, PaddingLength);

				// Check for empty stdin record
				if (ContentLength + PaddingLength == 0)
				{
					endOfRecord = offset + 8 - 1;
					return;
				}
			}
			else if (RecordType == RecordType.FCGIParams)
			{
				NamesAndValues = new NameValuePairCollection(ContentLength, PaddingLength);

				// Watch for empty content
				if (ContentLength + PaddingLength == 0)
				{
					endOfRecord = offset + 8 - 1;
					return;
				}
			}
			else
			{
				//TODO: Other types of records
				throw new NotImplementedException("");
			}

			FeedBytes(data, offset + 8, length - 8, out endOfRecord);
		}
		#endregion
	}
}
