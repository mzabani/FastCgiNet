using System;
using NUnit.Framework;
using System.Linq;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class RecordFactoryTests
  	{
		[Test]
		public void BeginRequestRecordInBlocks() {
			var beginRec = new BeginRequestRecord(1);

			BeginRequestRecord builtRecord = null;
			
			var byteReader = new RecordFactory();
			foreach (var recData in beginRec.GetBytes())
				builtRecord = (BeginRequestRecord) byteReader.Read(recData).SingleOrDefault();
			
			Assert.AreNotEqual(null, builtRecord);
			Assert.AreEqual(beginRec, builtRecord);
		}
	}
}
