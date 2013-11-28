using System;
using System.Linq;
using NUnit.Framework;
using System.IO;
using FastCgiNet.Streams;

namespace FastCgiNet.Tests
{
    [TestFixture]
    public class NvpWriterAndReaderTests
    {
        [Test]
        public void OneParameter()
        {
            using (var s = new FastCgiStreamImpl(false))
            {
                using (var writer = new NvpWriter(s))
                {
                    writer.Write("HELLO", "WORLD");

                    s.Position = 0;
                    using (var reader = new NvpReader(s))
                    {
                        var param = reader.Read();
                        Assert.AreEqual("HELLO", param.Name);
                        Assert.AreEqual("WORLD", param.Value);
                    }
                }
            }
        }

        [Test]
        public void ManyParameters()
        {
            var dict = new System.Collections.Generic.Dictionary<string, string> {
                { "HELLO" , "WORLD" },
                { "FASTCGINET IS", "AWESOME" },
                { "THIS TEST", "SHALL PASS" }
            };

            using (var s = new FastCgiStreamImpl(false))
            {
                using (var writer = new NvpWriter(s))
                {
                    foreach (var param in dict)
                        writer.Write(param.Key, param.Value);
                    
                    s.Position = 0;
                    using (var reader = new NvpReader(s))
                    {
                        NameValuePair param;
                        while ((param = reader.Read()) != null)
                        {
                            Assert.AreEqual(dict[param.Name], param.Value);
                        }
                    }
                }
            }
        }
    }
}
