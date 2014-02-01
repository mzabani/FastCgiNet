using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
    public class EndRequestRecordTests
	{
        [Test]
        public void SetAndGetNegativeApplicationStatus()
        {
            var rec = new EndRequestRecord(1);
            
            for (int status = -10; status < 0; ++status)
            {
                rec.AppStatus = status;
                Assert.AreEqual(status, rec.AppStatus);
            }
        }

		[Test]
		public void SetAndGetNonNegativeApplicationStatus()
        {
			var rec = new EndRequestRecord(1);

            for (int status = 20000000; status < 20000010; ++status)
            {
                rec.AppStatus = status;
                Assert.AreEqual(status, rec.AppStatus);
            }
		}

		[Test]
		public void SetAndGetProtocolStatus()
        {
			var rec = new EndRequestRecord(1);

            ProtocolStatus status = ProtocolStatus.CantMpxConn;

            rec.ProtocolStatus = status;
            Assert.AreEqual(status, rec.ProtocolStatus);
            
            status = ProtocolStatus.Overloaded;
            rec.ProtocolStatus = status;
            Assert.AreEqual(status, rec.ProtocolStatus);
            
            status = ProtocolStatus.RequestComplete;
            rec.ProtocolStatus = status;
            Assert.AreEqual(status, rec.ProtocolStatus);

            status = ProtocolStatus.UnknownRole;
            rec.ProtocolStatus = status;
            Assert.AreEqual(status, rec.ProtocolStatus);
		}
	}
}
