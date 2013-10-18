using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace Tests
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
			var nvp = new NameValuePair(data, offset, 100, out lastByteIdx);

			Assert.AreEqual(offset + 10, lastByteIdx);
			Assert.AreEqual("name", nvp.Name);
			Assert.AreEqual("value", nvp.Value);
		}

		[Test]
		public void TestNVPCollection() {
			int contentLength = 11 * 2;
			int paddingLength = 4;
			int offset = 0;
			byte[] data = new byte[100];

			// Two consecutive nvps
			GetNVP11(data, offset);
			GetNVP11(data, offset + 11);

			int lastByteOfRecord;
			var nvpCollection = new NameValuePairCollection(contentLength, paddingLength);
			nvpCollection.FeedBytes(data, offset, contentLength + paddingLength, out lastByteOfRecord);

			Assert.AreEqual(offset + contentLength + paddingLength - 1, lastByteOfRecord);

			var nvp1 = nvpCollection.Content.First();
			var nvp2 = nvpCollection.Content.ElementAt(1);

			Assert.AreEqual("name", nvp1.Name);
			Assert.AreEqual("name", nvp2.Name);
			Assert.AreEqual("value", nvp1.Value);
			Assert.AreEqual("value", nvp2.Value);
		}

		[Test]
		public void RecordContentsStreamWriteAndRead() {
			var s = new RecordContentsStream();

			byte[] testbuf = new byte[10];

			for (int i = 0; i < testbuf.Length; ++i)
				s.Write(testbuf, i, 1);

			byte[] readBytes = new byte[10];
			for (int i = 0; i < testbuf.Length; ++i)
			{
				s.Read(readBytes, i, 1);
				Assert.AreEqual(testbuf[i], readBytes[0]);
			}

			s.Read(readBytes, 0, 10);
			for (int i = 0; i < testbuf.Length; ++i)
				Assert.AreEqual(testbuf[i], readBytes[i]);

			Assert.AreEqual(10, s.Length);
		}

		[Test]
		public void RecordSocketSizeOfSentData() {
			byte[] data = new byte[1024];
			var record = new Record(RecordType.FCGIStdout, 1);
			Stream s = record.ContentStream;
			// Just write anything
			s.Write(data, 0, data.Length);

			int totalRecordBytes = 0;
			foreach (var arrSegment in record.GetBytes())
			{
				totalRecordBytes += arrSegment.Count;
			}

			Assert.AreEqual(data.Length + 8 + record.PaddingLength, totalRecordBytes);
		}
	}
}
