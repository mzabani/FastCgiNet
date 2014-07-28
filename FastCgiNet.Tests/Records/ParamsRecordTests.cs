using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;
using FastCgiNet.Streams;

namespace FastCgiNet.Tests
{
	[TestFixture]
    public class ParamsRecordTests
	{
		[Test]
		public void CreateAndReadParamsWithOneParameter()
        {
			using (var paramsRec = new ParamsRecord(1))
			{
				using (var writer = new NvpWriter(paramsRec.Contents))
                {
                    writer.Write("TEST", "WHATEVER");
                    
    				var bytes = paramsRec.GetBytes().ToList();
    				var header = bytes[0];
    				int endOfRecord;
    				using (var receivedRec = (ParamsRecord)RecordFactory.CreateRecordFromHeader(header.Array, null, header.Offset, header.Count, out endOfRecord))
    				{
    					Assert.AreEqual(paramsRec.ContentLength, receivedRec.ContentLength);
    					for (int i = 1; i < bytes.Count; ++i)
    					{
    						Assert.AreEqual(-1, endOfRecord);
    						receivedRec.FeedBytes(bytes[i].Array, bytes[i].Offset, bytes[i].Count, out endOfRecord);
    					}

                        receivedRec.Contents.Position = 0;
                        using (var reader = new NvpReader(receivedRec.Contents))
                        {
                            NameValuePair onlyParameterAdded = reader.Read();
        					Assert.AreEqual("TEST", onlyParameterAdded.Name);
        					Assert.AreEqual("WHATEVER", onlyParameterAdded.Value);
                        }
    				}
                }
			}
		}

		[Test]
        public void CreateAndReadParamsWithManyParameters() {
			int numParams = 100;
			using (var paramsRec = new ParamsRecord(1))
			{
                using (var writer = new NvpWriter(paramsRec.Contents))
                {
    				for (int i = 0; i < numParams; ++i)
    					writer.Write("TEST" + i, "WHATEVER" + i);
                }

				var bytes = paramsRec.GetBytes().ToList();
				var header = bytes[0];
				int endOfRecord;
				using (var receivedRec = (ParamsRecord)RecordFactory.CreateRecordFromHeader(header.Array, null, header.Offset, header.Count, out endOfRecord))
				{
					Assert.AreEqual(paramsRec.ContentLength, receivedRec.ContentLength);
					for (int i = 1; i < bytes.Count; ++i)
					{
						Assert.AreEqual(-1, endOfRecord);
						receivedRec.FeedBytes(bytes[i].Array, bytes[i].Offset, bytes[i].Count, out endOfRecord);
					}

					int paramsCount = 0;
                    receivedRec.Contents.Position = 0;
                    using (var reader = new NvpReader(receivedRec.Contents))
                    {
                        NameValuePair par;
    					while ((par = reader.Read()) != null)
    					{
    						Assert.AreEqual("TEST" + paramsCount, par.Name);
    						Assert.AreEqual("WHATEVER" + paramsCount, par.Value);
    						paramsCount++;
    					}
                    }

					Assert.AreEqual(numParams, paramsCount);
				}
			}
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
			Assert.Throws<ArgumentException>(() => {
				byte[] data = new byte[7];
				int endOfRecord;

				using (var rec = new StdoutRecord(data, 0, data.Length, out endOfRecord))
				{
				}
			});
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
