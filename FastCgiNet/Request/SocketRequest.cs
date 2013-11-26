using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	public delegate void SocketClosed();

	/// <summary>
	/// This class represents a FastCgi Request running over a socket. It provides easy ways to read data sent from the other party
    /// and to send data too.
	/// </summary>
	public class SocketRequest : FastCgiRequest, IDisposable
	{
		public Socket Socket { get; private set; }

        /// <summary>
        /// Indicates if we are on the webserver side of the request or on the application side.
        /// </summary>
        private bool WebServer;

        #region Streams
        private SocketStream paramsStream;
        public override FastCgiStream ParamsStream
        { 
            get
            {
                if (paramsStream == null)
                {
                    paramsStream = new SocketStream(Socket, RecordType.FCGIParams, !WebServer);
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
                    stdin = new SocketStream(Socket, RecordType.FCGIStdin, !WebServer);
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
                    stdout = new SocketStream(Socket, RecordType.FCGIStdout, WebServer);
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
                    stderr = new SocketStream(Socket, RecordType.FCGIStderr, WebServer);
                }
                
                return stderr;
            }
        }
        #endregion

        public override void SetBeginRequest(BeginRequestRecord rec)
        {
            base.SetBeginRequest(rec);
            ((SocketStream)ParamsStream).RequestId = RequestId;
            ((SocketStream)Stdin).RequestId = RequestId;
            ((SocketStream)Stdout).RequestId = RequestId;
            ((SocketStream)Stderr).RequestId = RequestId;
        }

		/// <summary>
		/// Warns when the socket for this request was closed by our side because <see cref="CloseSocket()"/> was called.
		/// </summary>
		public SocketClosed OnSocketClose = delegate {};

		/// <summary>
		/// Sends a record over the wire. This method does not throw if the socket has been closed.
		/// </summary>
		/// <returns>True if the record was sent successfuly, false if the socket was closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but the record couldn't be sent for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>
		public bool Send(RecordBase rec)
		{
			if (rec == null)
				throw new ArgumentNullException("rec");
			else if (rec.RequestId != RequestId)
				throw new ArgumentException("It is a necessary condition that the requestId of a record to be sent must match the connection's requestId");

			try
			{
				foreach (var arrSegment in rec.GetBytes())
				{
					Socket.Send(arrSegment.Array, arrSegment.Offset, arrSegment.Count, SocketFlags.None);
				}
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (SocketException e)
			{
				if (!SocketHelper.IsConnectionAbortedByTheOtherSide(e))
                    throw;

				return false;
			}

			return true;
		}

		/// <summary>
		/// Closes the socket from this connection safely, i.e. if it has already been closed, no exceptions happen.
        /// If the connection wasn't closed and was closed successfuly, OnSocketClose is triggered.
		/// </summary>
		/// <returns>True if the connection has been successfuly closed, false if it was already closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but couldn't be closed for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>
		public bool CloseSocket()
		{
			try
			{
				Socket.Close();
				Socket.Dispose();
				OnSocketClose();
				return true;
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException e)
			{
				if (!SocketHelper.IsConnectionAbortedByTheOtherSide(e))
					throw;
			}

			return false;
		}

        public override void Dispose()
        {
            CloseSocket();
            base.Dispose();
        }

		public override int GetHashCode()
		{
			// A request is uniquely identified by its socket and its requestid.
			return Socket.GetHashCode() + 71 * RequestId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var b = obj as SocketRequest;
			if (b == null)
				return false;

			return b.Socket.Equals(this.Socket) && b.RequestId.Equals(this.RequestId);
		}

		#region Constructors
		public SocketRequest(Socket s, bool webServer)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			
			this.Socket = s;
            WebServer = webServer;
		}

		public SocketRequest(Socket s, BeginRequestRecord beginRequestRecord, bool webServer)
			: this(s, webServer)
		{
			SetBeginRequest(beginRequestRecord);
 		}
		#endregion
	}
}
