using FastCgiNet.Streams;
using System;
using System.IO;
using System.Collections.Generic;

namespace FastCgiNet
{
	/// <summary>
	/// This class makes it easy for you to build FastCgi Records with bytes received through the communication medium (e.g. a socket).
	/// </summary>
	public class RecordFactory : IDisposable
	{
		/// <summary>
		/// Inside the Read loop situations may arise when we were left with less than 8 bytes to create a new record,
		/// in which case one can't be created. We save those bytes here for the time when we receive more data, so that
		/// maybe then we can create a record.
		/// </summary>
		private byte[] ReceivedButUnusedBytes = new byte[8];
		private int NumUnusedBytes = 0;

        /// <summary>
        /// The sum, in bytes, of the sizes of the contents of all records built so far (headers are not included).
        /// </summary>
        public long BuiltContentSize { get; private set; }

        private Stream secondaryStorageStream;
        /// <summary>
        /// The maximum size of all records' contents, in bytes, that this record factory will store in memory. Anything that surpasses this limit
        /// is stored in the supplied secondary storage stream.
        /// </summary>
        public readonly int MaxInMemoryContentSize;

		private RecordBase LastIncompleteRecord = null;
		/// <summary>
		/// Inputs FastCgi data sequentially with the purpose of building Records. Every call to this method can yield one or more Records, so watch it.
		/// Also, records yielded here are not disposed for you.
		/// </summary>
		/// <param name="data">The array with the read data. This will not be written to.</param>
		/// <param name="bytesRead">The number of bytes read and stored in <paramref name="data"/>.</param>
		public IEnumerable<RecordBase> Read(byte[] data, int offset, int bytesRead)
		{
			return Read(new ArraySegment<byte>(data, offset, bytesRead));
		}

		/// <summary>
		/// Inputs FastCgi socket data sequentially with the purpose of building Records. Every call to this method can yield one or more Records, so watch it.
		/// Also, records yielded here are not disposed for you.
		/// </summary>
		/// <param name="data">The array segment with the read data. This will not be modified nor will its underlying array be written to.</param>
		public IEnumerable<RecordBase> Read(ArraySegment<byte> data)
		{
			// Read them bytes and start the record building loop!
			int bytesRead = data.Count;
			int bytesFed = 0;
			while (bytesFed < bytesRead)
			{
				/*
				//TODO: Until we are 100% certain this loop works without any flaws, let's keep this so that we easily know
				// if something is wrong
				if (bytesFed > bytesRead)
					throw new InvalidOperationException("The loop is badly wrong. It feeds more bytes than it reads.");
				*/

				int bytesLeft = bytesRead - bytesFed;
				int lastByteOfRecord;
				
				// Are we going to create a new record or feed the last incomplete one?
				if (LastIncompleteRecord == null)
				{
					if (NumUnusedBytes + bytesLeft < 8)
					{
						// We still can't make a full header with what we have. Save and return.
						Array.Copy(data.Array, data.Offset + bytesFed, ReceivedButUnusedBytes, NumUnusedBytes, bytesLeft);
						NumUnusedBytes += bytesLeft;

						yield break;
					}
                    else if (NumUnusedBytes > 0)
                    {
                        // We should use these bytes that haven't been fed yet
                        int neededForFullHeader = 8 - NumUnusedBytes;
                        Array.Copy(data.Array, data.Offset + bytesFed, ReceivedButUnusedBytes, NumUnusedBytes, neededForFullHeader);

                        // We must not forget to store this record's contents in secondary storage if it's going to surpass our in-memory limit
                        LastIncompleteRecord = CreateRecordFromHeader(ReceivedButUnusedBytes, 0, 8, out lastByteOfRecord);
                        if (LastIncompleteRecord.ContentLength + BuiltContentSize > MaxInMemoryContentSize && LastIncompleteRecord.IsByteStreamRecord)
                            ((StreamRecordBase)LastIncompleteRecord).Contents = new RecordContentsStream(secondaryStorageStream);
                        if (bytesLeft - neededForFullHeader > 0)
                            LastIncompleteRecord.FeedBytes(data.Array, data.Offset + neededForFullHeader, bytesLeft - neededForFullHeader, out lastByteOfRecord);
                    }
                    else
                    {
                        // We must not forget to store this record's contents in secondary storage if it's going to surpass our in-memory limit
                        LastIncompleteRecord = CreateRecordFromHeader(data.Array, data.Offset + bytesFed, 8, out lastByteOfRecord);
                        if (LastIncompleteRecord.ContentLength + BuiltContentSize > MaxInMemoryContentSize && LastIncompleteRecord.IsByteStreamRecord)
                            ((StreamRecordBase)LastIncompleteRecord).Contents = new RecordContentsStream(secondaryStorageStream);
                        if (bytesLeft - 8 > 0)
                            LastIncompleteRecord.FeedBytes(data.Array, data.Offset + bytesFed + 8, bytesLeft - 8, out lastByteOfRecord);
                    }
				}
				else
				{
					LastIncompleteRecord.FeedBytes(data.Array, data.Offset + bytesFed, bytesLeft, out lastByteOfRecord);
				}
				
				// Check if we either created a complete record or fed that last incomplete one until its completion
				// If it is incomplete, then we must have fed all bytes read, and as such we should return.
				if (lastByteOfRecord == -1)
				{
					yield break;
				}

				RecordBase builtRecord = LastIncompleteRecord;
				LastIncompleteRecord = null;
				bytesFed = lastByteOfRecord + 1;
				
				// Tell all who are interested that we built a new record
                BuiltContentSize += builtRecord.ContentLength;
				yield return builtRecord;
			}
		}

        public void Dispose()
        {
            if (secondaryStorageStream != null)
                secondaryStorageStream.Dispose();
        }

        /// <summary>
        /// Instantiates a Record Factory that builds all its records in memory up to 2kB of built records' contents, then storing everything else
        /// in secondary storage.
        /// </summary>
        public RecordFactory()
            : this(new SecondaryStorageRequestStream(), 2048)
        {
        }

        /// <summary>
        /// Instantiates a Record Factory that builds all its records in memory up to 2kB of built records' contents, then storing everything else
        /// in secondary storage.
        /// </summary>
        /// <param name="maxInMemoryContentSize">
        /// The maximum size of all records' contents, in bytes, that this record factory will store in memory. Anything that surpasses this limit
        /// is stored in the supplied secondary storage stream.
        /// </param>
        public RecordFactory(int maxInMemoryContentSize)
            : this(new SecondaryStorageRequestStream(), maxInMemoryContentSize)
        {
        }

        /// <summary>
        /// Instantiates a Record Factory that builds all its records (both header and contents) in memory as long as the summed size of those records'
        /// contents does not surpass <paramref name="maxContentSize"/>. All other records' contents (not their headers) are then stored in secondary storage.
        /// </summary>
        /// <param name="secondaryStorageStream">
        /// The stream that will be used to store records' contents after the specified in-memory limit. The life-cycle of this stream
        /// _is_ controlled by this Record Factory. This means that it will be disposed when this record factory is disposed.
        /// </param>
        /// <param name="maxInMemoryContentSize">
        /// The maximum size of all records' contents, in bytes, that this record factory will store in memory. Anything that surpasses this limit
        /// is stored in the supplied secondary storage stream.
        /// </param>
        public RecordFactory(Stream secondaryStorageStream, int maxInMemoryContentSize)
        {
            if (secondaryStorageStream == null)
                throw new ArgumentNullException("secondaryStorageOps");
            if (maxInMemoryContentSize < 0)
                throw new ArgumentOutOfRangeException("The maximum in memory content size must be non negative");
            if (!secondaryStorageStream.CanSeek)
                throw new ArgumentException("The supplied secondary storage stream must be seekable");

            BuiltContentSize = 0;
            this.secondaryStorageStream = secondaryStorageStream;
            this.MaxInMemoryContentSize = maxInMemoryContentSize;
        }

        #region Static methods
        /// <summary>
        /// Returns an instance of the appropriate Record class according to <paramref name="recordType"/>, defining
        /// that the created record's contents will be stored in secondary storage (only available for some types of Stream Records).
        /// </summary>
        public static RecordBase CreateRecord(ushort requestId, RecordType recordType, Stream secondaryStorageStream)
        {
            if (secondaryStorageStream == null)
                throw new ArgumentNullException("secondaryStorageOps");

            if (recordType == RecordType.FCGIBeginRequest)
            {
                return new BeginRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIEndRequest)
            {
                return new EndRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIStdin)
            {
                return new StdinRecord(requestId, secondaryStorageStream);
            }
            else if (recordType == RecordType.FCGIStdout)
            {
                return new StdoutRecord(requestId);
            }
            else if (recordType == RecordType.FCGIStderr)
            {
                return new StderrRecord(requestId);
            }
            else if (recordType == RecordType.FCGIParams)
            {
                return new ParamsRecord(requestId);
            }
            else if (recordType == RecordType.FCGIAbortRequest)
            {
                return new AbortRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIData)
            {
                return new DataRecord(requestId, secondaryStorageStream);
            }
            else
            {
                /*
        FCGIGetValues,
        FCGIGetValuesResult*/
                throw new NotImplementedException("Record of type " + recordType + " is not implemented yet");
                //TODO: Other types of records
            }
        }
        /// <summary>
        /// Returns an instance of the appropriate Record class according to <paramref name="recordType"/>.
        /// </summary>
        public static RecordBase CreateRecord(ushort requestId, RecordType recordType)
        {
            if (recordType == RecordType.FCGIBeginRequest)
            {
                return new BeginRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIEndRequest)
            {
                return new EndRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIStdin)
            {
                return new StdinRecord(requestId);
            }
            else if (recordType == RecordType.FCGIStdout)
            {
                return new StdoutRecord(requestId);
            }
            else if (recordType == RecordType.FCGIStderr)
            {
                return new StderrRecord(requestId);
            }
            else if (recordType == RecordType.FCGIParams)
            {
                return new ParamsRecord(requestId);
            }
            else if (recordType == RecordType.FCGIAbortRequest)
            {
                return new AbortRequestRecord(requestId);
            }
            else if (recordType == RecordType.FCGIData)
            {
                return new DataRecord(requestId);
            }
            else
            {
                /*
        FCGIGetValues,
        FCGIGetValuesResult*/
                throw new NotImplementedException("Record of type " + recordType + " is not implemented yet");
                //TODO: Other types of records
            }
        }
        
        internal static RecordBase CreateRecordFromHeader(byte[] header, int offset, int length, out int endOfRecord)
        {
            if (offset + 8 > header.Length)
                throw new InsufficientBytesException("There are not enough bytes in the array for a complete header. Make sure at least 8 bytes are available");
            if (ByteUtils.CheckArrayBounds(header, offset, length) == false)
                throw new InvalidOperationException("Array bounds are wrong");
            
            var recordType = (RecordType)header[offset + 1];
            
            if (recordType == RecordType.FCGIBeginRequest)
            {
                return new BeginRequestRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIEndRequest)
            {
                return new EndRequestRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIStdin)
            {
                return new StdinRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIStdout)
            {
                return new StdoutRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIStderr)
            {
                return new StderrRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIParams)
            {
                return new ParamsRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIAbortRequest)
            {
                return new AbortRequestRecord(header, offset, length, out endOfRecord);
            }
            else if (recordType == RecordType.FCGIData)
            {
                return new DataRecord(header, offset, length, out endOfRecord);
            }
            else
            {
                /*
        FCGIGetValues,
        FCGIGetValuesResult*/
                throw new NotImplementedException("Record of type " + recordType + " is not implemented yet");
                //TODO: Other types of records
            }
        }
        #endregion
	}
}
