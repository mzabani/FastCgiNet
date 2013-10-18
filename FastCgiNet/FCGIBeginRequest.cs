using System;
using FastCgiNet;

namespace FastCgiNet
{
	public class FCGIBeginRequest
	{
		byte[] roleAndFlags;
		int fedBytes;

		public Role Role
		{
			get
			{
				return (Role)((roleAndFlags[0] << 8) + roleAndFlags[1]);
			}
		}

		public bool ApplicationMustCloseConnection
		{
			get
			{
				return (roleAndFlags[2] & 1) == 0;
			}
		}

		internal void FeedBytes(byte[] contentData, int offset, int length, out int endOfRecord)
		{
			if (fedBytes == 8)
				throw new InvalidOperationException("The BeginRequest section of this record is complete.");
			else if (ByteCopyUtils.CheckArrayBounds(contentData, offset, length) == false)
				throw new InvalidOperationException("Would go out of bounds");

			int bytesNeeded = 8 - fedBytes;
			if (length >= bytesNeeded)
			{
				Array.Copy(contentData, offset, roleAndFlags, fedBytes, bytesNeeded);
				fedBytes = 8;
				endOfRecord = offset + bytesNeeded - 1;
			}
			else
			{
				Array.Copy(contentData, offset, roleAndFlags, fedBytes, length);
				fedBytes += length;
				endOfRecord = -1;
			}
		}

		internal FCGIBeginRequest()
		{
			roleAndFlags = new byte[8];
			fedBytes = 0;
		}
	}
}
