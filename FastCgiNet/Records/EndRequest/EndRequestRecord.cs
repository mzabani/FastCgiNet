using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class EndRequestRecord : RecordBase
	{
		byte[] AppAndProtocolStatus;
		int fedBytes;
		
		public int AppStatus
		{
			get
			{
				return (AppAndProtocolStatus[0] << 24) | (AppAndProtocolStatus[1] << 16) | (AppAndProtocolStatus[2] << 8) | AppAndProtocolStatus[3];
			}
            set
            {
                // MSB is byte index 0, LSB is byte index 3
                AppAndProtocolStatus[0] = (byte) (value & 0xFF000000);
                AppAndProtocolStatus[1] = (byte) (value & 0xFF0000);
                AppAndProtocolStatus[2] = (byte) (value & 0xFF00);
                AppAndProtocolStatus[3] = (byte) (value & 0xFF);
            }
		}
		public ProtocolStatus ProtocolStatus
		{
			get
			{
				return (ProtocolStatus)AppAndProtocolStatus[4];
			}
			set
			{
				AppAndProtocolStatus[4] = (byte) value;
			}
		}
		
		public override IEnumerable<ArraySegment<byte>> GetBytes()
		{
			yield return CalculatePaddingAndGetHeaderBytes();

			yield return new ArraySegment<byte>(AppAndProtocolStatus);

			foreach (var seg in GetPaddingBytes())
				yield return seg;
		}
		
		internal override void FeedBytes(byte[] data, int offset, int length, out int endOfRecord)
		{
			if (fedBytes == 5)
				throw new InvalidOperationException("The EndRequest section of this record is complete.");
			AssertArrayOperation(data, offset, length);
			
			int bytesNeeded = 5 - fedBytes;
			if (length >= bytesNeeded)
			{
				Array.Copy(data, offset, AppAndProtocolStatus, fedBytes, bytesNeeded);
				fedBytes = 5;
				endOfRecord = offset + bytesNeeded - 1;
			}
			else
			{
				Array.Copy(data, offset, AppAndProtocolStatus, fedBytes, length);
				fedBytes += length;
				endOfRecord = -1;
			}
		}

		public EndRequestRecord(ushort requestId)
			: base(RecordType.FCGIEndRequest, requestId)
		{
			AppAndProtocolStatus = new byte[5];
			fedBytes = 0;
			ContentLength = 0;
		}

		internal EndRequestRecord(byte[] data, int offset, int length, out int endOfRecord)
			: base(data, offset, length, RecordType.FCGIEndRequest)
		{
			AppAndProtocolStatus = new byte[5];
			fedBytes = 0;

			if (length > 8)
				FeedBytes(data, offset + 8, length - 8, out endOfRecord);
			else
				endOfRecord = -1;
		}
	}
}
