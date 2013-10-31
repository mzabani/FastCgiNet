using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;
using System.Reflection;
using System.Collections.Generic;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class GetBytesFromRecord
	{
		[Test]
		public void FirstSegmentIsHeader()
		{
			using (var rec = new StdinRecord(1))
			{
				// rec is just an empty record
				var header = rec.GetBytes().First();

				Assert.AreEqual(8, header.Count);
			}
		}
	}
}

