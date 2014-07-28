using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class EndOfRecord
	{
		[Test]
		public void EmptyStdinRecord()
		{
			using (var rec = new StdinRecord(1))
			{
				// rec is just an empty record
				var header = rec.GetBytes().First();
				int endOfRecord;
				using (var emptyRec = new StdinRecord(header.Array, null, header.Offset, header.Count, out endOfRecord))
				{
					Assert.AreEqual(7, endOfRecord);
				}
			}
		}

		[Test]
		public void NonEmptyStdinRecord()
		{
			using (var rec = new StdinRecord(1))
			{
				byte[] x = new byte[1]; // The contents of the array don't really matter
				rec.Contents.Write(x, 0, 1);

				var blocks = rec.GetBytes().ToList();
				var header = blocks.First();
				int endOfRecord;
				using (var emptyRec = new StdinRecord(header.Array, null, header.Offset, header.Count, out endOfRecord))
				{
					Assert.AreEqual(-1, endOfRecord);
					emptyRec.FeedBytes(blocks[1].Array, blocks[1].Offset, blocks[1].Count, out endOfRecord);
					Assert.AreEqual(0, endOfRecord);
				}
			}
		}
	}
}
