using System;
using System.IO;
using FastCgiNet;
using FastCgiNet.Streams;
using FastCgiNet.Requests;
using System.Net;
using System.Net.Sockets;

namespace PerformanceTests {
    public static class PerformanceApp {
        private static System.Net.IPAddress ListenAddr = System.Net.IPAddress.Loopback;
        private static int ListenPort = 9007;
        
        /// <summary>
        /// Maximum Poll or Select time to wait for new connections or data in loopback sockets before it is considered
        /// an error.
        /// </summary>
        private static int MaxPollTime = 100000;

        public static int Main(string[] args) {
            int numIterations;
            if (!int.TryParse(args [0], out numIterations))
            {
                Console.WriteLine("Usage: mono program.exe numIterations");
                return -1;
            }
            Console.WriteLine("We are going to run a routine {0} times. Please wait...", numIterations);

            for (int i = 0; i < numIterations; i++)
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
                                webserverRequest.SendBeginRequest(Role.Responder, true);
                                using (var nvpWriter = new FastCgiNet.Streams.NvpWriter(webserverRequest.Params))
                                {
                                    nvpWriter.Write("HELLO", "WORLD");
                                }
                                
                                if (!applicationSocket.Poll(MaxPollTime, SelectMode.SelectRead))
                                    throw new Exception("Data took too long");
                                
                                using (var applicationRequest = new ApplicationSocketRequest(applicationSocket))
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
                                            endRequestReceived = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Done.");
            return 0;
        }
    
        private static Socket GetListenSocket()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(ListenAddr, ListenPort));
            sock.Listen(1);
            return sock;
        }
        
        private static Socket GetWebserverConnectedSocket()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ConnectAsync(new SocketAsyncEventArgs {
                RemoteEndPoint = new System.Net.IPEndPoint(ListenAddr, ListenPort),
                SocketFlags = SocketFlags.None
            });
            return sock;
        }
    }
}

