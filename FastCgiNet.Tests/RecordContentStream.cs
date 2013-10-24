using System;
using FastCgiNet;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RecordContentStream
	{
		[Test]
		public void WriteAndRead() {
			var s = new RecordContentsStream();
			
			byte[] testbuf = new byte[10];
			
			for (int i = 0; i < testbuf.Length; ++i)
				s.Write(testbuf, i, 1);
			
			byte[] readBytes = new byte[10];
			for (int i = 0; i < testbuf.Length; ++i)
			{
				s.Read(readBytes, i, 1);
				Assert.AreEqual(testbuf[i], readBytes[0]);
			}
			
			s.Read(readBytes, 0, 10);
			for (int i = 0; i < testbuf.Length; ++i)
				Assert.AreEqual(testbuf[i], readBytes[i]);
			
			Assert.AreEqual(10, s.Length);
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
			try
			{
				s.Write(anything, 0, 1);
			}
			catch (InvalidOperationException)
			{
			}
		}
	}
}
