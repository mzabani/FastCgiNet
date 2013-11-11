using System;
using NUnit.Framework;
using System.Linq;
using FastCgiNet.Logging;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class ByteReaderTests
	{
		//TODO: Stress test the byte reader. Pass in less than 8 bytes, wait a while, pass the remaining bytes.
		// Do all sorts of nasty combinations of byte passing.

		[Test]
		public void ParamsRecordManyParametersByteByByte() {
			int numParams = 100;
			using (var paramsRec = new ParamsRecord(1))
			{
				for (int i = 0; i < numParams; ++i)
					paramsRec.Add("TEST" + i, "WHATEVER" + i);

				ParamsRecord builtRecord = null;

				var byteReader = new ByteReader(new RecordFactory());
				foreach (var recData in paramsRec.GetBytes())
				{
					for (int i = 0; i < recData.Count; ++i)
					{
						builtRecord = (ParamsRecord)byteReader.Read(recData.Array, recData.Offset + i, 1).SingleOrDefault();
					}
				}

				Assert.AreNotEqual(null, builtRecord);
				Assert.AreEqual(paramsRec, builtRecord);
				Assert.AreEqual(paramsRec.ContentLength, builtRecord.ContentLength);
				
				int paramsCount = 0;
				foreach (var par in builtRecord.Parameters)
				{
					Assert.AreEqual("TEST" + paramsCount, par.Name);
					Assert.AreEqual("WHATEVER" + paramsCount, par.Value);
					paramsCount++;
				}
				
				Assert.AreEqual(numParams, paramsCount);
			}
		}

		[Test]
		public void BeginRequestRecordInBlocks() {
			var beginRec = new BeginRequestRecord(1);

			BeginRequestRecord builtRecord = null;
			
			var byteReader = new ByteReader(new RecordFactory());
			foreach (var recData in beginRec.GetBytes())
				builtRecord = (BeginRequestRecord) byteReader.Read(recData).SingleOrDefault();
			
			Assert.AreNotEqual(null, builtRecord);
			Assert.AreEqual(beginRec, builtRecord);
		}
	}
}
