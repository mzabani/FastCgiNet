using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet.Requests
{
	/// <summary>
	/// This class represents a FastCgi Request running over a socket, from the point of view of the FastCgi application (not the webserver). It provides easy ways to read data sent from the webserver
    /// and to send data too.
	/// </summary>
	public class ApplicationSocketRequest : SocketRequest
	{
        public Role Role { get; private set; }
        public bool ApplicationMustCloseConnection { get; private set; }

        #region Streams
        private SocketStream dataStream;
        public override FastCgiStream Data
        { 
            get
            {
                if (dataStream == null)
                {
                    dataStream = new SocketStream(Socket, RecordType.FCGIData, true);
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
                    paramsStream = new SocketStream(Socket, RecordType.FCGIParams, true);
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
                    stdin = new SocketStream(Socket, RecordType.FCGIStdin, true);
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
                    stdout = new SocketStream(Socket, RecordType.FCGIStdout, false);
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
                    stderr = new SocketStream(Socket, RecordType.FCGIStderr, false);
                }
                
                return stderr;
            }
        }
        #endregion

        protected override void AddReceivedRecord(RecordBase rec)
        {
            base.AddReceivedRecord(rec);

            var beginRec = rec as BeginRequestRecord;
            if (beginRec != null)
            {
                Role  = beginRec.Role;
                ApplicationMustCloseConnection = beginRec.ApplicationMustCloseConnection;
            }
        }

        /// <summary>
        /// After writing to the output Streams, you have to end the request with a Status Code and a protocol status.
        /// Use this method to do that before disposing this object.
        /// </summary>
        /// <param name="appStatus">The Application status. Use 0 for success and anything else for error.</param>
        public void SendEndRequest(int appStatus, ProtocolStatus protocolStatus)
        {
            // Flush stuff before doing this!
            Data.Dispose();
            Params.Dispose();
            Stdin.Dispose();
            Stdout.Dispose();
            Stderr.Dispose();

            var rec = new EndRequestRecord(RequestId);
            rec.AppStatus = appStatus;
            rec.ProtocolStatus = protocolStatus;

            Send(rec);
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
        /// Initializes a FastCgi Request over a socket that represents the point of view of the application (not of the webserver).
        /// All received records' contents' are stored in memory.
        /// </summary>
        [Obsolete("Use the constructor that needs a RecordFactory")]
		public ApplicationSocketRequest(Socket s)
            : base(s)
		{
		}

        /// <summary>
        /// Initializes a FastCgi Request over a socket that represents the point of view of the application (not of the webserver).
        /// All received records' contents' are stored in memory.
        /// </summary>
        [Obsolete("Use the constructor that needs a RecordFactory")]
		public ApplicationSocketRequest(Socket s, BeginRequestRecord beginRequestRecord)
			: this(s)
		{
			AddReceivedRecord(beginRequestRecord);
 		}

        /// <summary>
        /// Initializes a FastCgi Request over a socket that represents the point of view of the application (not of the webserver).
        /// The supplied <paramref name="recordFactory"/> is used to build the records that represent the incoming data.
        /// </summary>
        /// <param name="recordFactory">
        /// The factory used to create records. This object's life cycle is controlled by this request.
        /// That means that when this request is disposed, so will this record factory be.
        /// </param>
        public ApplicationSocketRequest(Socket s, RecordFactory recordFactory)
            : base(s, recordFactory)
        {
        }
	}
}
