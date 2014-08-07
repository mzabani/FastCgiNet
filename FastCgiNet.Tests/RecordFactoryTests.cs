using System;
using NUnit.Framework;
using System.Linq;
using System.IO;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class RecordFactoryTests
  	{
		[Test]
		public void BuildingRecords()
        {
            // After supplying the factory with a record's bytes, the same record must come out
			var beginRec = new BeginRequestRecord(1);
            beginRec.Role = Role.Responder;
            var stdinRec = new StdinRecord(1);
            stdinRec.Contents.WriteByte(0);
            stdinRec.Contents.WriteByte(0);

			BeginRequestRecord builtBeginReqRecord = null;
            StdinRecord builtStdinRecord = null;
			
			var recFactory = new RecordFactory();
			foreach (var recData in beginRec.GetBytes())
				builtBeginReqRecord = (BeginRequestRecord) recFactory.Read(recData).SingleOrDefault();
            foreach (var recData in stdinRec.GetBytes())
                builtStdinRecord = (StdinRecord) recFactory.Read(recData).SingleOrDefault();
			
			Assert.AreEqual(beginRec, builtBeginReqRecord);
            Assert.AreEqual(stdinRec, builtStdinRecord);
		}

        [Test]
        public void BuiltContentSizeIsCorrect()
        {
            // When building records, the BuiltContentSize property must be carefully maintained
            var anyRec = new StdinRecord(1);
            anyRec.Contents.WriteByte(1);
            anyRec.Contents.WriteByte(2);

            var recFactory = new RecordFactory();

            Assert.AreEqual(0, recFactory.BuiltContentSize);

            foreach (var recData in anyRec.GetBytes())
                recFactory.Read(recData).SingleOrDefault();

            Assert.AreEqual(2, recFactory.BuiltContentSize);

            foreach (var recData in anyRec.GetBytes())
                recFactory.Read(recData).SingleOrDefault();

            Assert.AreEqual(4, recFactory.BuiltContentSize);
        }

        [Test]
        public void BuildingRecordWithSecondaryStorage()
        {
            // After supplying the factory with a record's bytes, the same record must come out, even
            // when using secondary storage
            var anyRec = new StdinRecord(1);
            anyRec.Contents.WriteByte(1);
            anyRec.Contents.WriteByte(2);
            
            StdinRecord builtRecord = null;

            var secondaryStorageStream = new MemoryStream();
            var recFactory = new RecordFactory(secondaryStorageStream, 0);
            foreach (var recData in anyRec.GetBytes())
                builtRecord = (StdinRecord) recFactory.Read(recData).SingleOrDefault();

            Assert.IsNull(builtRecord.Contents.MemoryBlocks);
            Assert.AreEqual(anyRec, builtRecord);
        }

        [Test]
        public void MaximumInMemoryBuiltContentSizeIsRespected()
        {
            // We need to know that the max in memory content size is respected
            var record = new StdinRecord(1);
            record.Contents.WriteByte(0);

            StdinRecord inSecStorageBuiltRecord = null;
            
            var secondaryStorageStream = new MemoryStream();
            var recFactory = new RecordFactory(secondaryStorageStream, 0);
            foreach (var recData in record.GetBytes())
                inSecStorageBuiltRecord = (StdinRecord) recFactory.Read(recData).SingleOrDefault();

            Assert.IsTrue(inSecStorageBuiltRecord.Contents.InSecondaryStorage);
            Assert.AreEqual(record, inSecStorageBuiltRecord);
        }

        [Test]
        public void MaximumInMemoryBuiltContentSizeIsRespectedWithInMemoryRecord()
        {
            // We need to know that the max in memory content size is respected. This time, one of the records
            // will be stored in memory
            var firstRec = new StdinRecord(1);
            firstRec.Contents.WriteByte(0);

            var secondRec = new StdinRecord(2);
            secondRec.Contents.WriteByte(0);
            secondRec.Contents.WriteByte(0);
            
            StdinRecord inMemoryBuiltRecord = null;
            StdinRecord inSecStorageBuiltRecord = null;
            
            var secondaryStorageStream = new MemoryStream();
            var recFactory = new RecordFactory(secondaryStorageStream, 2);
            foreach (var recData in firstRec.GetBytes())
                inMemoryBuiltRecord = (StdinRecord) recFactory.Read(recData).SingleOrDefault();
            foreach (var recData in secondRec.GetBytes())
                inSecStorageBuiltRecord = (StdinRecord) recFactory.Read(recData).SingleOrDefault();

            Assert.IsFalse(inMemoryBuiltRecord.Contents.InSecondaryStorage);
            Assert.IsTrue(inSecStorageBuiltRecord.Contents.InSecondaryStorage);
            Assert.AreEqual(firstRec, inMemoryBuiltRecord);
            Assert.AreEqual(secondRec, inSecStorageBuiltRecord);
        }
	}
}
