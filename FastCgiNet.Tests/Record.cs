using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class Record
	{
		[Test]
		public void ParamsRecordOneParameter() {
			using (var paramsRec = new ParamsRecord(1))
			{
				paramsRec.Add("TEST", "WHATEVER");

				var bytes = paramsRec.GetBytes().ToList();
				var header = bytes[0];
				var recFactory = new RecordFactory();
				int endOfRecord;
				using (var receivedRec = (ParamsRecord)recFactory.CreateRecordFromHeader(header.Array, header.Offset, header.Count, out endOfRecord))
				{
					Assert.AreEqual(paramsRec.ContentLength, receivedRec.ContentLength);
					for (int i = 1; i < bytes.Count; ++i)
					{
						Assert.AreEqual(-1, endOfRecord);
						receivedRec.FeedBytes(bytes[i].Array, bytes[i].Offset, bytes[i].Count, out endOfRecord);
					}

					NameValuePair onlyParameterAdded = receivedRec.Parameters.First();
					Assert.AreEqual("TEST", onlyParameterAdded.Name);
					Assert.AreEqual("WHATEVER", onlyParameterAdded.Value);
				}
			}
		}

		[Test]
		public void ParamsRecordManyParameters() {
			int numParams = 100;
			using (var paramsRec = new ParamsRecord(1))
			{
				for (int i = 0; i < numParams; ++i)
					paramsRec.Add("TEST" + i, "WHATEVER" + i);
				
				var bytes = paramsRec.GetBytes().ToList();
				var header = bytes[0];
				var recFactory = new RecordFactory();
				int endOfRecord;
				using (var receivedRec = (ParamsRecord)recFactory.CreateRecordFromHeader(header.Array, header.Offset, header.Count, out endOfRecord))
				{
					Assert.AreEqual(paramsRec.ContentLength, receivedRec.ContentLength);
					for (int i = 1; i < bytes.Count; ++i)
					{
						Assert.AreEqual(-1, endOfRecord);
						receivedRec.FeedBytes(bytes[i].Array, bytes[i].Offset, bytes[i].Count, out endOfRecord);
					}

					int paramsCount = 0;
					foreach (var par in receivedRec.Parameters)
					{
						Assert.AreEqual("TEST" + paramsCount, par.Name);
						Assert.AreEqual("WHATEVER" + paramsCount, par.Value);
						paramsCount++;
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
