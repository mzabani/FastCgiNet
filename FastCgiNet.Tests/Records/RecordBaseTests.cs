using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class RecordBaseTests
	{
		[Test]
		public void NewAllTypesOfRecords() {
			RecordBase rec = new BeginRequestRecord(1);
			Assert.AreEqual(RecordType.FCGIBeginRequest, rec.RecordType);

			rec = new EndRequestRecord(1);
			Assert.AreEqual(RecordType.FCGIEndRequest, rec.RecordType);

			rec = new StdinRecord(1);
			Assert.AreEqual(RecordType.FCGIStdin, rec.RecordType);

			rec = new StdoutRecord(1);
			Assert.AreEqual(RecordType.FCGIStdout, rec.RecordType);

			rec = new StderrRecord(1);
			Assert.AreEqual(RecordType.FCGIStderr, rec.RecordType);

			rec = new ParamsRecord(1);
			Assert.AreEqual(RecordType.FCGIParams, rec.RecordType);

			rec = new BeginRequestRecord(1);
			Assert.AreEqual(RecordType.FCGIBeginRequest, rec.RecordType);

            rec = new DataRecord(1);
            Assert.AreEqual(RecordType.FCGIData, rec.RecordType);
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
	}
}
