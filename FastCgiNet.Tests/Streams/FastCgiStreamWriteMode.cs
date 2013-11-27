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
        public void BasicProperties()
        {
            using (var s = new FastCgiStreamImpl(false))
            {
                byte[] buf = new byte[1];
                Assert.IsFalse(s.IsReadMode);
                Assert.IsTrue(s.CanWrite);
                Assert.IsTrue(s.CanSeek);
                Assert.Throws<InvalidOperationException>(() => { bool a = s.IsComplete; });
                s.Write(buf, 0, buf.Length);
                Assert.AreEqual(buf.Length, s.Length);
            }
        }

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
				Assert.AreEqual(numStreams + 1, s.UnderlyingStreams.Count());
				Assert.AreEqual(0, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize, s.Length);
				s.Write(hugeChunk, 0, 1);
				Assert.AreEqual(numStreams + 1, s.UnderlyingStreams.Count());
				Assert.AreEqual(1, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize + 1, s.Length);
			}
		}
	}
}
