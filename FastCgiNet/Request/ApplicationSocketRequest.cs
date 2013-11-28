using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet
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

        public void SendEndRequest(int appStatus, ProtocolStatus protocolStatus)
        {
            var rec = new EndRequestRecord(RequestId);
            rec.AppStatus = appStatus;
            rec.ProtocolStatus = protocolStatus;

            Send(rec);
        }

        public override void Dispose()
        {
            CloseSocket(); // Just to signal OnSocketClose
            Socket.Dispose();
            base.Dispose();
        }

		#region Constructors
		public ApplicationSocketRequest(Socket s)
            : base(s)
		{
		}

		public ApplicationSocketRequest(Socket s, BeginRequestRecord beginRequestRecord)
			: this(s)
		{
			AddReceivedRecord(beginRequestRecord);
 		}
		#endregion
	}
}
