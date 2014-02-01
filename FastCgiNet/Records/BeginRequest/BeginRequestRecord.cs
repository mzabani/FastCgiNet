using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class BeginRequestRecord : RecordBase
	{
		private byte[] RoleAndFlags;
		private int fedBytes;
		
		public Role Role
		{
			get
			{
				return (Role)((RoleAndFlags[0] << 8) + RoleAndFlags[1]);
			}
			set
			{
				// MSB is byte index 0, LSB is byte index 1
				ushort role = (ushort)value;
				RoleAndFlags[0] = (byte) ((role & 0xFF00) >> 8);
				RoleAndFlags[1] = (byte) (role & 0xFF);
			}
		}
		
		public bool ApplicationMustCloseConnection
		{
			get
			{
				return (RoleAndFlags[2] & 1) == 0;
			}
			set
			{
				RoleAndFlags[2] = (byte) (value ? 0 : 1);
			}
		}

		public override IEnumerable<ArraySegment<byte>> GetBytes()
		{
			yield return CalculatePaddingAndGetHeaderBytes();

			yield return new ArraySegment<byte>(RoleAndFlags);

			foreach (var seg in GetPaddingBytes())
				yield return seg;
		}

		internal override void FeedBytes(byte[] data, int offset, int length, out int endOfRecord)
		{
			if (fedBytes == 8)
				throw new InvalidOperationException("The BeginRequest section of this record is complete.");
			AssertArrayOperation(data, offset, length);
			
			int bytesNeeded = 8 - fedBytes;
			if (length >= bytesNeeded)
			{
				Array.Copy(data, offset, RoleAndFlags, fedBytes, bytesNeeded);
				fedBytes = 8;
				endOfRecord = offset + bytesNeeded - 1;
			}
			else
			{
				Array.Copy(data, offset, RoleAndFlags, fedBytes, length);
				fedBytes += length;
				endOfRecord = -1;
			}
		}

		public BeginRequestRecord(ushort requestId)
			: base(RecordType.FCGIBeginRequest, requestId)
		{
			RoleAndFlags = new byte[8];
			fedBytes = 0;
			ContentLength = 0;
		}

		internal BeginRequestRecord(byte[] data, int offset, int length, out int endOfRecord)
			: base(data, offset, length, RecordType.FCGIBeginRequest)
		{
			RoleAndFlags = new byte[8];
			fedBytes = 0;

			if (length > 8)
				FeedBytes(data, offset + 8, length - 8, out endOfRecord);
			else
				endOfRecord = -1;
		}
	}
}
