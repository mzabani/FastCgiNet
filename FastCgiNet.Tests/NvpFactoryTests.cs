using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class NvpFactoryTests
	{
		private void GetNVP11(byte[] buffer, int offset)
		{
			buffer[offset] = 4;
			buffer[offset + 1] = 5;
			
			buffer[offset + 2] = (byte)'n';
			buffer[offset + 3] = (byte)'a';
			buffer[offset + 4] = (byte)'m';
			buffer[offset + 5] = (byte)'e';
			
			buffer[offset + 6] = (byte)'v';
			buffer[offset + 7] = (byte)'a';
			buffer[offset + 8] = (byte)'l';
			buffer[offset + 9] = (byte)'u';
			buffer[offset + 10] = (byte)'e';
		}
		
		[Test]
		public void CreateBytesFromNvp11 ()
		{
			var rec = new NameValuePair("name", "value");
			
			int i = 0;
			byte[] buf = new byte[50];
			GetNVP11(buf, 0);
			foreach (var seg in rec.GetBytes())
			{
				Assert.That(ByteUtils.SegmentsEqual(seg, new ArraySegment<byte>(buf, i, seg.Count)));
				i += seg.Count;
			}
		}
		
		[Test]
		public void CreateNvp11FromBytes ()
		{
			byte[] data = new byte[100];
			int offset = 3;
			
			GetNVP11(data, offset);
			
			int lastByteIdx;
			NameValuePair nvp;
			bool createdNvp = NvpFactory.TryCreateNVP(data, offset, 100 - offset, out nvp, out lastByteIdx);
			
			Assert.AreEqual(true, createdNvp);
			
			Assert.AreEqual(offset + 10, lastByteIdx);
			Assert.AreEqual("name", nvp.Name);
			Assert.AreEqual("value", nvp.Value);
		}
	}
}
