using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;

namespace FastCgiNet.Tests
{
	[TestFixture]
	public class BeginRequestRecordTests
	{
		[Test]
		public void SetAndGetRole() {
			var rec = new BeginRequestRecord(1);

			var role = Role.Authorizer;
			rec.Role = role;
			Assert.AreEqual(role, rec.Role);

			role = Role.Filter;
			rec.Role = role;
			Assert.AreEqual(role, rec.Role);

			role = Role.Responder;
			rec.Role = role;
			Assert.AreEqual(role, rec.Role);
		}

		[Test]
		public void SetAndGetAppMustClose() {
			var rec = new BeginRequestRecord(1);
			
			rec.ApplicationMustCloseConnection = true;
			Assert.AreEqual(true, rec.ApplicationMustCloseConnection);

			rec.ApplicationMustCloseConnection = false;
			Assert.AreEqual(false, rec.ApplicationMustCloseConnection);
		}
	}
}
