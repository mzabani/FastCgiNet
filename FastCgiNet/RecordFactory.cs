using System;

namespace FastCgiNet
{
	public class RecordFactory
	{
		public RecordBase CreateRecordFromHeader(byte[] header, int offset, int length, out int endOfRecord)
		{
			if (offset + 8 > header.Length)
				throw new InvalidOperationException("There are not enough bytes in the array for a complete header. Make sure at least 8 bytes are passed");
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
				throw new NotImplementedException("Record of type " + recordType + " is not implemented yet");
				//TODO: Other types of records
			}
		}
	}
}

