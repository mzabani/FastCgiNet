using FastCgiNet.Streams;
using System;
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

        private ISecondaryStorageOps secondaryStorageOps;
        private int maxContentSize;

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
                    // If (when) we need to create a record, should we use secondary storage?
                    ISecondaryStorageOps secondaryStorageToBeUsed = BuiltContentSize >= maxContentSize ? secondaryStorageOps : null;

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

						LastIncompleteRecord = CreateRecordFromHeader(ReceivedButUnusedBytes, secondaryStorageToBeUsed, 0, 8, out lastByteOfRecord);
						if (bytesLeft - neededForFullHeader > 0)
							LastIncompleteRecord.FeedBytes(data.Array, data.Offset + neededForFullHeader, bytesLeft - neededForFullHeader, out lastByteOfRecord);
					}
					else
					{
                        LastIncompleteRecord = CreateRecordFromHeader(data.Array, secondaryStorageToBeUsed, data.Offset + bytesFed, bytesLeft, out lastByteOfRecord);
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
            if (secondaryStorageOps != null)
                secondaryStorageOps.Dispose();
        }

        /// <summary>
        /// Instantiates a Record Factory that builds all its records in memory.
        /// </summary>
        public RecordFactory()
        {
            BuiltContentSize = 0;
        }

        /// <summary>
        /// Instantiates a Record Factory that builds all its records (both header and contents) in memory until the summed size of all
        /// built records in bytes goes over <paramref name="maxRequestSize"/>, then storing other records' contents in secondary storage
        /// (the headers and other basic properties are still stored in memory).
        /// </summary>
        /// <param name="secondaryStorageOps">
        /// The implementation of <see cref="ISecondaryStorageOps"/> that will be used to store records' contents
        /// after the specified limit. The life-cycle of this object controlled by this Record Factory. This means that it will
        /// be disposed when this record factory is disposed.
        /// <param name="maxRequestSize">
        /// The maximum size of all previously built records' contents, in bytes, so that future records' contents
        /// are stored in secondary storage.
        /// </param>
        public RecordFactory(ISecondaryStorageOps secondaryStorageOps, int maxContentSize)
        {
            if (secondaryStorageOps == null)
                throw new ArgumentNullException("secondaryStorageOps");
            if (maxContentSize < 0)
                throw new ArgumentOutOfRangeException("The maximum content size must be non negative");

            BuiltContentSize = 0;
            this.secondaryStorageOps = secondaryStorageOps;
            this.maxContentSize = maxContentSize;
        }

        #region Static methods
        /// <summary>
        /// Returns an instance of the appropriate Record class according to <paramref name="recordType"/>, defining
        /// that the created record's contents will be stored in secondary storage (only available for some types of Stream Records).
        /// </summary>
        public static RecordBase CreateRecord(ushort requestId, RecordType recordType, ISecondaryStorageOps secondaryStorageOps)
        {
            if (secondaryStorageOps == null)
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
                return new StdinRecord(requestId, secondaryStorageOps);
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
                return new DataRecord(requestId, secondaryStorageOps);
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
        
        internal static RecordBase CreateRecordFromHeader(byte[] header, ISecondaryStorageOps secondaryStorageOps, int offset, int length, out int endOfRecord)
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
                return new StdinRecord(header, secondaryStorageOps, offset, length, out endOfRecord);
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
                return new DataRecord(header, secondaryStorageOps, offset, length, out endOfRecord);
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
