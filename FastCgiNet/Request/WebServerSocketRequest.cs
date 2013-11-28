using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	/// <summary>
	/// This class represents a FastCgi Request running over a socket, from the point of view of the WebServer. It provides easy ways to read data sent from the FastCgi application
    /// and to send data too.
	/// </summary>
    public class WebServerSocketRequest : SocketRequest
	{
        /// <summary>
        /// The status code returned by the application.
        /// </summary>
        public int AppStatus { get; private set; }

        /// <summary>
        /// The Protocol Status returned by the application.
        /// </summary>
        public ProtocolStatus ProtocolStatus { get; private set; }

        #region Streams
        private SocketStream paramsStream;
        public override FastCgiStream Params
        { 
            get
            {
                if (paramsStream == null)
                {
                    paramsStream = new SocketStream(Socket, RecordType.FCGIParams, false);
                    paramsStream.RequestId = RequestId;
                }
                
                return paramsStream;
            }
        }
        private SocketStream stdin;
        public override FastCgiStream Stdin
        { 
            get
            {
                if (stdin == null)
                {
                    stdin = new SocketStream(Socket, RecordType.FCGIStdin, false);
                    stdin.RequestId = RequestId;
                }

                return stdin;
            }
        }
        private SocketStream stdout;
        public override FastCgiStream Stdout
        { 
            get
            {
                if (stdout == null)
                {
                    stdout = new SocketStream(Socket, RecordType.FCGIStdout, true);
                    stdout.RequestId = RequestId;
                }
                
                return stdout;
            }
        }
        private SocketStream stderr;
        public override FastCgiStream Stderr
        { 
            get
            {
                if (stderr == null)
                {
                    stderr = new SocketStream(Socket, RecordType.FCGIStderr, true);
                    stderr.RequestId = RequestId;
                }
                
                return stderr;
            }
        }
        #endregion

        public void SendBeginRequest(Role applicationRole, bool applicationMustCloseConnection)
        {
            var beginRec = new BeginRequestRecord(RequestId);
            beginRec.Role = applicationRole;
            beginRec.ApplicationMustCloseConnection = applicationMustCloseConnection;
//            Role = applicationRole;
//            ApplicationMustCloseConnection = applicationMustCloseConnection;
            Send(beginRec);
        }

        /// <summary>
        /// If you don't have a request body to send, don't write to <see cref="Stdin"/> and call this method. It will also dispose
        /// <see cref="Stdin"/>.
        /// </summary>
        public void SendEmptyStdin()
        {
            using (var emptyStdin = new StdinRecord(RequestId))
            {
                Send(emptyStdin);
            }

            Stdin.Dispose();
        }

        public WebServerSocketRequest(Socket s, ushort requestId)
            : base(s)
		{
            RequestId = requestId;
 		}
	}
}
