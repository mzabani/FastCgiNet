using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	/// <summary>
	/// This is the base class for FastCgi Requests running over a socket.
	/// </summary>
    public abstract class SocketRequest : FastCgiRequest
	{
		protected Socket Socket { get; private set; }

        protected override void AddReceivedRecord(RecordBase rec)
        {
            base.AddReceivedRecord(rec);

            var beginRequestRec = rec as BeginRequestRecord;
            if (beginRequestRec == null)
                return;

            ((SocketStream)Params).RequestId = beginRequestRec.RequestId;
            ((SocketStream)Stdin).RequestId = beginRequestRec.RequestId;
            ((SocketStream)Stdout).RequestId = beginRequestRec.RequestId;
            ((SocketStream)Stderr).RequestId = beginRequestRec.RequestId;
        }

		/// <summary>
		/// Sends a record over the wire. This method could throw if the connection has been closed prematurely by the other side or if the socket
        /// is in the process of being closed by your code or is already closed.
		/// </summary>
        /// <exception cref="System.ObjectDisposedException">If the Socket was disposed.</exception>
        /// <exception cref="System.SocketException">If the Socket was prematurely closed by the other side or was in the process of being closed.</exception>
        /*/// <returns>True if the record was sent successfuly, false if the socket was closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but the record couldn't be sent for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>*/
		protected override void Send(RecordBase rec)
		{
            base.Send(rec);

            foreach (var arrSegment in rec.GetBytes())
            {
                Socket.Send(arrSegment.Array, arrSegment.Offset, arrSegment.Count, SocketFlags.None);
            }

            /*
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

			return true;*/
		}

		/*/// <summary>
		/// Closes the socket from this connection safely, i.e. if it has already been closed, no exceptions happen.
        /// If the connection wasn't closed and was closed successfuly, OnSocketClose is triggered.
		/// </summary>
		/// <returns>True if the connection has been successfuly closed, false if it was already closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but couldn't be closed for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>*/
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
            Socket.Dispose();
            //OnSocketClose();
            /*
            // If the socket has already been closed, just return false
            if (!Socket.Connected)
                return false;

			try
			{
                //Socket.Shutdown(SocketShutdown.Receive);
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

			return false;*/
		}

        public override void Dispose()
        {
            CloseSocket(); // Just to signal OnSocketClose
            base.Dispose();
        }

        /*
		public override int GetHashCode()
		{
			// A request is uniquely identified by its socket and its requestid.
			return Socket.GetHashCode() + 71 * RequestId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var b = obj as ApplicationSocketRequest;
			if (b == null)
				return false;

			return b.Socket.Equals(this.Socket) && b.RequestId.Equals(this.RequestId);
		}*/

		public SocketRequest(Socket s)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			
			this.Socket = s;
		}
	}
}
