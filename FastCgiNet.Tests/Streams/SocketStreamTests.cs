using System;
using System.Linq;
using NUnit.Framework;
using System.IO;
using FastCgiNet.Streams;
using System.Net.Sockets;

namespace FastCgiNet.Tests
{
    [TestFixture]
    public class SocketStreamTests
    {
        private Socket GetSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        [Test]
        public void WriteReadModeThrows()
        {
            using (var s = new SocketStream(GetSocket(), RecordType.FCGIStdin, true))
            {
                byte[] buf = new byte[1];
                Assert.Throws<InvalidOperationException>(() => s.Write(buf, 0, 1));
            }
        }

        [Test]
        public void DisposeReadModeEmptyStream()
        {
            // No error should happen, because flushing and closing a read mode stream does nothing
            using (var s = new SocketStream(GetSocket(), RecordType.FCGIStdin, true))
            {
            }
        }

        [Test]
        public void FlushReadModeStreamThrows()
        {
            using (var s = new SocketStream(GetSocket(), RecordType.FCGIStdin, true))
            {
                Assert.Throws<InvalidOperationException>(() => s.Flush());
            }
        }

        [Test]
        public void DisposeReadModeEmptyStreamMoreThanOnce()
        {
            SocketStream s;
            using (s = new SocketStream(GetSocket(), RecordType.FCGIStdin, true))
            {
            }
            
            s.Dispose();
            s.Dispose();
        }

        [Test]
        public void DisposeWriteModeEmptyStream()
        {
            // No error should happen, because flushing and closing the stream does not dispose or close the socket,
            // nor does it send any records
            using (var s = new SocketStream(GetSocket(), RecordType.FCGIStdin, false))
            {
            }
        }

        [Test]
        public void FlushAndDisposeWriteModeEmptyStreamMoreThanOnce()
        {
            // No error should happen, because flushing and closing the stream does not dispose or close the socket,
            // nor does it send any records
            SocketStream s;
            using (s = new SocketStream(GetSocket(), RecordType.FCGIStdin, false))
            {
                s.Flush();
                s.Flush();
                s.Flush();
            }

            s.Dispose();
            s.Dispose();

            Assert.Throws<ObjectDisposedException>(() => s.Flush());

            s.Dispose();
        }
    }
}
