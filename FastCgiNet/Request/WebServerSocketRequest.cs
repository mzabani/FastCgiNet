using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet.Requests
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

        /// <summary>
        /// Determines if the application's response is complete.
        /// </summary>
        public bool ResponseComplete
        {
            get
            {
                return EndRequestReceived;
            }
        }

        #region Streams
        private SocketStream dataStream;
        public override FastCgiStream Data
        { 
            get
            {
                if (dataStream == null)
                {
                    dataStream = new SocketStream(Socket, RecordType.FCGIData, false);
                }
                
                return dataStream;
            }
        }
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

        /// <summary>
        /// Closes the socket of this request. This method could throw if the connection has been closed prematurely by the other side or if the socket
        /// is in the process of being closed by your code.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">If the Socket was disposed.</exception>
        /// <exception cref="System.SocketException">If the Socket was prematurely closed by the other side or was in the process of being closed.</exception>
        protected virtual void CloseSocket()
        {
            if (Socket.Connected == false)
                return;
            
            Socket.Close();
        }

        public override void Dispose()
        {
            CloseSocket();
            Socket.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Initializes a FastCgi Request over a socket that represents the point of view of the webserver (not of the application).
        /// </summary>
        public WebServerSocketRequest(Socket s, ushort requestId)
            : base(s)
		{
            RequestId = requestId;
 		}
	}
}
