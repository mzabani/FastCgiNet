using System;
using System.Linq;
using NUnit.Framework;
using System.IO;

namespace FastCgiNet.Tests
{
	[TestFixture]
    public class FastCgiStreamWriteMode
  	{
		[Test]
		public void LengthAndNumberOfStreamsCheck()
		{
			using (var s = new FastCgiStreamImpl(false))
			{
				int numStreams = 3;

				int chunkSize = 65535 * numStreams;
				byte[] hugeChunk = new byte[chunkSize];
				Assert.AreEqual(0, s.Length);
				s.Write(hugeChunk, 0, chunkSize);
				Assert.AreEqual(numStreams, s.UnderlyingStreams.Count());
				Assert.AreEqual(65535, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize, s.Length);
				s.Write(hugeChunk, 0, 1);
				Assert.AreEqual(numStreams + 1, s.UnderlyingStreams.Count ());
				Assert.AreEqual(1, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize + 1, s.Length);
			}
		}
	}
}
