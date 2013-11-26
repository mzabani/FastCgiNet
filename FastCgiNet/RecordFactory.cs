using System;
using FastCgiNet.Logging;
using System.Collections.Generic;

namespace FastCgiNet
{
	/// <summary>
	/// This class makes it easy for you to build FastCgi Records with bytes received through the communication medium.
	/// </summary>
	public class RecordFactory
	{
		//private ILogger Logger;

		/// <summary>
		/// Inside the Read loop situations may arise when we were left with less than 8 bytes to create a new record,
		/// in which case one can't be created. We save those bytes here for the time when we receive more data, so that
		/// maybe then we can create a record.
		/// </summary>
		private byte[] ReceivedButUnusedBytes = new byte[8];
		private int NumUnusedBytes = 0;

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
						
						/*if (Logger != null)
							Logger.Debug("Not enough bytes in the header ({0}) to create a record yet", NumUnusedBytes);*/

						yield break;
					}
					else if (NumUnusedBytes > 0)
					{
						// We should use these bytes that haven't been fed yet
						/*if (Logger != null)
							Logger.Debug("Creating new record with {0} bytes still to be fed", NumUnusedBytes + bytesLeft);*/

						int neededForFullHeader = 8 - NumUnusedBytes;
						Array.Copy(data.Array, data.Offset + bytesFed, ReceivedButUnusedBytes, NumUnusedBytes, neededForFullHeader);

						LastIncompleteRecord = CreateRecordFromHeader(ReceivedButUnusedBytes, 0, 8, out lastByteOfRecord);
						if (bytesLeft - neededForFullHeader > 0)
							LastIncompleteRecord.FeedBytes(data.Array, data.Offset + neededForFullHeader, bytesLeft - neededForFullHeader, out lastByteOfRecord);
					}
					else
					{
						/*if (Logger != null)
							Logger.Debug("Creating new record with {0} bytes still to be fed", bytesLeft);*/

						LastIncompleteRecord = CreateRecordFromHeader(data.Array, data.Offset + bytesFed, bytesLeft, out lastByteOfRecord);
					}
				}
				else
				{
					/*if (Logger != null)
						Logger.Debug("Feeding bytes into last incomplete record");*/
					
					LastIncompleteRecord.FeedBytes(data.Array, data.Offset + bytesFed, bytesLeft, out lastByteOfRecord);
				}
				
				// Check if we either created a complete record or fed that last incomplete one until its completion
				// If it is incomplete, then we must have fed all bytes read, and as such we should return.
				if (lastByteOfRecord == -1)
				{
					/*if (Logger != null)
						Logger.Debug("Record is still incomplete.");*/

					yield break;
				}

				/*if (Logger != null)
					Logger.Debug("Record is complete. Setting last incomplete record to null");*/
				RecordBase builtRecord = LastIncompleteRecord;
				LastIncompleteRecord = null;
				bytesFed = lastByteOfRecord + 1;
				
				// Tell all who are interested that we built a new record
				yield return builtRecord;
			}
		}

        /*
		public void SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			Logger = logger;
		}*/

        #region Static methods
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
            else
            {
                /*
             * FCGIData,
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
            else
            {
                /*
             * FCGIData,
        FCGIGetValues,
        FCGIGetValuesResult*/
                throw new NotImplementedException("Record of type " + recordType + " is not implemented yet");
                //TODO: Other types of records
            }
        }
        #endregion

		public RecordFactory()
		{
			//Logger = null;
		}
	}
}
