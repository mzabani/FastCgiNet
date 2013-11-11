using System;
using FastCgiNet;
using NUnit.Framework;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class RecordContentStream
	{
		[Test]
		public void WriteSeekAndReadPositionLength() {
			var s = new RecordContentsStream();
			
			byte[] testbuf = new byte[10];
			testbuf[0] = (byte)1;
			testbuf[5] = (byte)7;
			
			for (int i = 0; i < testbuf.Length; ++i)
				s.Write(testbuf, i, 1);

			s.Seek(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(testbuf.Length, s.Length);
			Assert.AreEqual(0, s.Position);

			s.Position = 5;
			s.Read(testbuf, 0, 1);
			Assert.AreEqual(6, s.Position);
			Assert.AreEqual(testbuf.Length, s.Length);
			Assert.AreEqual(testbuf[0], testbuf[5]);
			Assert.AreEqual(testbuf[0], (byte)7);
		}

		[Test]
		public void WritePositionLength() {
			var s = new RecordContentsStream();
			
			byte[] testbuf = new byte[10];
			
			for (int i = 0; i < testbuf.Length; ++i)
				s.Write(testbuf, i, 1);
			
			Assert.AreEqual(testbuf.Length, s.Length);
			Assert.AreEqual(testbuf.Length, s.Position);
		}

		[Test]
		public void OnlyWriteAtEndOfStream()
		{
			var s = new RecordContentsStream();
			
			byte[] testbuf = new byte[10];
			
			s.Write(testbuf, 0, testbuf.Length);
			s.Position = 0;

			Assert.Throws<NotImplementedException>(() => {
				s.Write(testbuf, 0, 1);
			});
		}

		[Test]
		public void WriteMoreThanLimit()
		{
			var s = new RecordContentsStream();

			// 65535 bytes is the limit of the contents of a record

			byte[] anything = new byte[1024];
			for (int i = 0; i < 64; ++i)
				s.Write(anything, 0, i == 63 ? 1023 : 1024);

			// Exception now!
			Assert.Throws<InvalidOperationException>(() => s.Write(anything, 0, 1));
		}
	}
}
