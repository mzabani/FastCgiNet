using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class Test
	{
		void GetNVP11(byte[] buffer, int offset)
		{
			buffer[offset] = 4;
			buffer[offset + 1] = 5;
			
			buffer[offset + 2] = (byte)'n';
			buffer[offset + 3] = (byte)'a';
			buffer[offset + 4] = (byte)'m';
			buffer[offset + 5] = (byte)'e';
			
			buffer[offset + 6] = (byte)'v';
			buffer[offset + 7] = (byte)'a';
			buffer[offset + 8] = (byte)'l';
			buffer[offset + 9] = (byte)'u';
			buffer[offset + 10] = (byte)'e';
		}

		void GetNVP(byte[] buffer, int offset, string name, string value)
		{
			throw new NotImplementedException();

			/*if (name.Length > 0x7f)
			{
				buffer[offset++] = (name.Length >> 24) | 0x100;
				buffer[offset++] = name.Length >> 16;
				buffer[offset++] = name.Length >> 8;
				buffer[offset++] = name.Length >> 24;
			}*/
		}

		[Test]
		public void TestNameValuePair11 ()
		{
			byte[] data = new byte[100];
			int offset = 3;

			GetNVP11(data, offset);

			int lastByteIdx;
			NameValuePair nvp;
			bool createdNvp = NVPFactory.TryCreateNVP(data, offset, 100 - offset, out nvp, out lastByteIdx);

			Assert.AreEqual(true, createdNvp);

			Assert.AreEqual(offset + 10, lastByteIdx);
			Assert.AreEqual("name", nvp.Name);
			Assert.AreEqual("value", nvp.Value);
		}

		[Test]
		public void ParamsRecord() {

		}

		[Test]
		public void RecordSocketSizeOfSentData() {
			byte[] data = new byte[1024];
			using (var record = new StdoutRecord(1))
			{
				Stream s = record.Contents;
				// Just write anything
				s.Write(data, 0, data.Length);

				int totalRecordBytes = record.GetBytes().Sum (d => d.Count);

				Assert.AreEqual(data.Length + 8 + record.PaddingLength, totalRecordBytes);
			}
		}
	
		[Test]
		public void TryToCreateRecordWithLessThanHeaderBytes()
		{
			byte[] data = new byte[7];
			int endOfRecord;
			try
			{
				using (var rec = new StdoutRecord(data, 0, data.Length, out endOfRecord))
				{
				}
			}
			catch (ArgumentException)
			{
			}
		}

		[Test]
		public void ReceiveEmptyRecord()
		{
			using (var rec = new StdinRecord(1))
			{
				var allRecordBytes = rec.GetBytes().ToList();

				// Header first
				var recordHeader = allRecordBytes[0];

				int endOfRecord;

				var factory = new RecordFactory();

				using (var receivedRecord = (StdinRecord)factory.CreateRecordFromHeader(recordHeader.Array, recordHeader.Offset, recordHeader.Count, out endOfRecord))
				{
					int i = 1;
					while (endOfRecord == -1)
					{
						recordHeader = allRecordBytes[i];
						receivedRecord.FeedBytes(recordHeader.Array, recordHeader.Offset, recordHeader.Count, out endOfRecord);
					}

					Assert.AreEqual(8 + receivedRecord.PaddingLength - 1, endOfRecord);
					Assert.AreEqual(0, receivedRecord.ContentLength);
					Assert.AreEqual(true, receivedRecord.EmptyContentData);
					Assert.AreEqual(true, receivedRecord.IsByteStreamRecord);
				}
			}
		}
	}
}
