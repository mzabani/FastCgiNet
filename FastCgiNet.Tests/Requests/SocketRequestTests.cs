using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using FastCgiNet;
using FastCgiNet.Requests;
using System.Net;
using System.Net.Sockets;

namespace FastCgiNet.Tests
{
    [TestFixture]
    public class SocketRequestTests
    {
        private System.Net.IPAddress ListenAddr = System.Net.IPAddress.Loopback;
        private int ListenPort = 9007;

        /// <summary>
        /// Maximum Poll or Select time to wait for new connections or data in loopback sockets before it is considered
        /// an error.
        /// </summary>
        private int MaxPollTime = 100000;

        private Socket GetListenSocket()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(ListenAddr, ListenPort));
            sock.Listen(1);
            return sock;
        }

        private Socket GetWebserverConnectedSocket()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ConnectAsync(new SocketAsyncEventArgs {
                RemoteEndPoint = new System.Net.IPEndPoint(ListenAddr, ListenPort),
                SocketFlags = SocketFlags.None
            });
            return sock;
        }

        [Test]
        public void WebserverAndApplicationConnectAndBeginRequestBasicProperties()
        {
            using (var listenSocket = GetListenSocket())
            {
                using (var webserverSocket = GetWebserverConnectedSocket())
                {
                    // Wait for the connection
                    if (!listenSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                        throw new Exception("Connection took too long");

                    using (var applicationSocket = listenSocket.Accept())
                    {
                        byte[] buf = new byte[128];
                        ushort requestId = 2;
                        using (var webserverRequest = new WebServerSocketRequest(webserverSocket, requestId))
                        {
                            Assert.AreEqual(requestId, webserverRequest.RequestId);
                            webserverRequest.SendBeginRequest(Role.Responder, true);
                            using (var nvpWriter = new FastCgiNet.Streams.NvpWriter(webserverRequest.Params))
                            {
                                nvpWriter.Write("HELLO", "WORLD");
                            }

                            if (!applicationSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                                throw new Exception("Data took too long");

                            using (var applicationRequest = new ApplicationSocketRequest(applicationSocket, new RecordFactory(int.MaxValue)))
                            {
                                int bytesRead;
                                bool beginRequestReceived = false;
                                while (true)
                                {
                                    if (beginRequestReceived)
                                        break;

                                    bytesRead = applicationSocket.Receive(buf);
                                    if (bytesRead == 0)
                                        throw new Exception("BeginRequest was not received");

                                    foreach (var rec in applicationRequest.FeedBytes(buf, 0, bytesRead))
                                    {
                                        if (rec is BeginRequestRecord)
                                        {
                                            Assert.AreEqual(requestId, applicationRequest.RequestId);
                                            Assert.AreEqual(Role.Responder, applicationRequest.Role);
                                            Assert.IsTrue(applicationRequest.ApplicationMustCloseConnection);
                                            beginRequestReceived = true;
                                        }
                                    }
                                }

                                applicationRequest.SendEndRequest(0, ProtocolStatus.RequestComplete);
                                applicationSocket.Close();
                            }

                            if (!webserverSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                                throw new Exception("Data took too long");

                            bool endRequestReceived = false;
                            while (true)
                            {
                                if (endRequestReceived)
                                    break;
                                
                                int bytesRead = webserverSocket.Receive(buf);
                                if (bytesRead == 0)
                                    throw new Exception("EndRequest was not received");
                                
                                foreach (var rec in webserverRequest.FeedBytes(buf, 0, bytesRead))
                                {
                                    var endRec = rec as EndRequestRecord;
                                    if (endRec != null)
                                    {
                                        Assert.AreEqual(requestId, rec.RequestId);
                                        Assert.AreEqual(0, endRec.AppStatus);
                                        Assert.AreEqual(ProtocolStatus.RequestComplete, endRec.ProtocolStatus);
                                        endRequestReceived = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        [Test]
        public void WebserverAndApplicationLongStdoutCommunication()
        {
            // The stdout stream will have the following written: "abcdefghijklmnopq " over and over again
            // Make sure this amounts to more than one Record
            var stdoutContentsBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < 10000; ++i)
            {
                stdoutContentsBuilder.Append("abcdefghijklmnopq ");
            }
            string stdoutContents = stdoutContentsBuilder.ToString();

            using (var listenSocket = GetListenSocket())
            {
                using (var webserverSocket = GetWebserverConnectedSocket())
                {
                    // Wait for the connection
                    if (!listenSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                        throw new Exception("Connection took too long");
                    
                    using (var applicationSocket = listenSocket.Accept())
                    {
                        byte[] buf = new byte[128];
                        ushort requestId = 2;
                        using (var webserverRequest = new WebServerSocketRequest(webserverSocket, requestId))
                        {
                            webserverRequest.SendBeginRequest(Role.Responder, true);
                            
                            using (var nvpWriter = new FastCgiNet.Streams.NvpWriter(webserverRequest.Params))
                            {
                                nvpWriter.WriteParamsFromUri(new Uri("http://github.com/mzabani"), "GET");
                            }
                            
                            if (!applicationSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                                throw new Exception("Data took too long");
                            
                            using (var applicationRequest = new ApplicationSocketRequest(applicationSocket))
                            {
                                int bytesRead;
                                bool beginRequestReceived = false;
                                while (true)
                                {
                                    if (beginRequestReceived && applicationRequest.Params.IsComplete)
                                        break;
                                    
                                    bytesRead = applicationSocket.Receive(buf);
                                    if (bytesRead == 0)
                                        throw new Exception("Read 0 bytes");
                                    
                                    foreach (var rec in applicationRequest.FeedBytes(buf, 0, bytesRead))
                                    {
                                        if (rec.RecordType == RecordType.FCGIBeginRequest)
                                            beginRequestReceived = true;
                                    }
                                }

                                // Write a whole bunch to Stdout.
                                using (var writer = new StreamWriter(applicationRequest.Stdout))
                                {
                                    writer.Write(stdoutContents);
                                }
                                
                                applicationRequest.SendEndRequest(0, ProtocolStatus.RequestComplete);
                                applicationSocket.Close();
                            }
                            
                            if (!webserverSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                                throw new Exception("Data took too long");
                            
                            bool endRequestReceived = false;
                            while (true)
                            {
                                if (endRequestReceived)
                                    break;
                                
                                int bytesRead = webserverSocket.Receive(buf);
                                if (bytesRead == 0)
                                    throw new Exception("Read 0 bytes");
                                
                                foreach (var rec in webserverRequest.FeedBytes(buf, 0, bytesRead))
                                {
                                    var endRec = rec as EndRequestRecord;
                                    if (endRec != null)
                                    {
                                        endRequestReceived = true;
                                    }
                                }
                            }

                            // Read stdout and make sure it got here in one piece!
                            Assert.IsTrue(webserverRequest.Stdout.IsComplete);
                            using (var reader = new StreamReader(webserverRequest.Stdout))
                            {
                                Assert.AreEqual(stdoutContents, reader.ReadToEnd());
                            }
                        }
                    }
                }
            }
        }
    }
}
