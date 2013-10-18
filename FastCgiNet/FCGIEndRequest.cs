using System;
using FastCgiNet;

namespace FastCgiNet
{
	public class FCGIEndRequest
	{
		byte[] appAndProtocolStatus;
		int fedBytes;

		public int AppStatus
		{
			get
			{
				return (appAndProtocolStatus[0] << 24) + (appAndProtocolStatus[1] << 16) + (appAndProtocolStatus[2] << 8) + appAndProtocolStatus[3];
			}
		}
		public ProtocolStatus ProtocolStatus
		{
			get
			{
				return (ProtocolStatus)appAndProtocolStatus[4];
			}
			set
			{
				appAndProtocolStatus[4] = (byte) value;
			}
		}

		internal byte[] GetBytes()
		{
			return appAndProtocolStatus;
		}

		internal void FeedBytes (byte[] contentData, int offset, int length, out int endOfRecord)
		{
			if (fedBytes == 5)
				throw new InvalidOperationException("The EndRequest section of this record is complete.");
			else if (ByteCopyUtils.CheckArrayBounds(contentData, offset, length) == false)
				throw new InvalidOperationException("Would go out of bounds");
			
			int bytesNeeded = 5 - fedBytes;
			if (length >= bytesNeeded)
			{
				Array.Copy(contentData, offset, appAndProtocolStatus, fedBytes, bytesNeeded);
				fedBytes = 5;
				endOfRecord = offset + bytesNeeded - 1;
			}
			else
			{
				Array.Copy(contentData, offset, appAndProtocolStatus, fedBytes, length);
				fedBytes += length;
				endOfRecord = -1;
			}
		}

		internal FCGIEndRequest()
		{
			appAndProtocolStatus = new byte[5];
			fedBytes = 0;
		}
	}
}

