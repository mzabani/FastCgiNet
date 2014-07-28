using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
    public class StreamRecordTests
    {
        [Test]
        public void RecordGetBytesDosNotChangeContentsStreamPosition()
        {
            byte[] data = new byte[1024];
            using (var record = new StdoutRecord(1))
            {
                // Just write anything and seek to some position
                long seekPos = 27;
                Stream s = record.Contents;
                s.Write(data, 0, data.Length);
                s.Position = seekPos;
                
                int totalRecordBytes = record.GetBytes().Sum(d => d.Count);
                
                Assert.AreEqual(data.Length + 8 + record.PaddingLength, totalRecordBytes);
                Assert.AreEqual(seekPos, s.Position);
            }
        }

		[Test]
		public void RecordGetBytes()
        {
			byte[] data = new byte[1024];
			using (var record = new StdoutRecord(1))
			{
				Stream s = record.Contents;
				// Just write anything
				s.Write(data, 0, data.Length);

				int totalRecordBytes = record.GetBytes().Sum(d => d.Count);

				Assert.AreEqual(data.Length + 8 + record.PaddingLength, totalRecordBytes);
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

				using (var receivedRecord = (StdinRecord)RecordFactory.CreateRecordFromHeader(recordHeader.Array, null, recordHeader.Offset, recordHeader.Count, out endOfRecord))
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
