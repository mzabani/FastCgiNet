using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace FastCgiNet
{
	public delegate void SocketClosed(Request req, bool abrupt);

	/// <summary>
	/// This class identifies a FastCgi request uniquely in time.
	/// </summary>
	public class Request
	{
		public ushort RequestId { get; private set; }
		public Socket Socket { get; private set; }

		/// <summary>
		/// Warns when the socket for this request is closed because <see cref="CloseSocket()"/> was called.
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
				if (e.SocketErrorCode != SocketError.Shutdown)
					throw;

				return false;
			}

			return true;
		}

		/// <summary>
		/// Closes the socket from this connection safely, i.e. if it has already been closed, no exceptions happen.
		/// </summary>
		/// <returns>True if the connection has been successfuly closed, false if it was already closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but couldn't be closed for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>
		public bool CloseSocket()
		{
			bool connectionClosedError = true;

			try
			{
				Socket.Close();
				Socket.Dispose();
				connectionClosedError = false;
				OnSocketClose(this, false);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException e)
			{
				if (e.SocketErrorCode != SocketError.Shutdown)
					throw;
			}

			// If the connection was already closed, then this is abrupt termination.
			OnSocketClose(this, true);

			return !connectionClosedError;
		}

		/// <summary>
		/// Sets some basic properties of this request such as its <see cref="RequestId"/>. After this, this object's identity
		/// will be preserved until this object's disposal.
		/// </summary>
		/// <param name="rec">The BeginRequest record.</param> 
		public void SetBeginRequest(BeginRequestRecord rec)
		{
			if (rec == null)
				throw new ArgumentNullException("rec");

			RequestId = rec.RequestId;
		}

		public override int GetHashCode ()
		{
			// A request is uniquely identified by its socket and its requestid.
			return Socket.GetHashCode() + 71 * RequestId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var b = obj as Request;
			if (b == null)
				return false;

			return b.Socket.Equals(this.Socket) && b.RequestId.Equals(this.RequestId);;
		}

		#region Constructors
		public Request(Socket s)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			
			this.Socket = s;
		}

		public Request(Socket s, BeginRequestRecord beginRequestRecord)
			: this(s)
		{
			SetBeginRequest(beginRequestRecord);
		}

		/*
		/// <summary>
		/// This constructor helps maintain a table of requests.
		/// </summary>
		/// <remarks>This object WILL change its identity once a BeginRequest record has been associated to it. This means
		/// <see cref="GetHashCode()"/> and <see cref="Equals(object b)"/> WILL behave differently after a call to 
	    /// <see cref="SetBeginRequest(Record rec)"/>. This is not exposed in the public API.</remarks>
		internal Request (Socket s, ConcurrentDictionary<Socket, Request> requestsTable, ILogger logger)
			: this(s, logger)
		{
			if (requestsTable == null)
				throw new ArgumentNullException("requestsTable");
			
			this.RequestsTable = requestsTable;
		}

		/// <summary>
		/// This constructor helps maintain a table of requests.
		/// </summary>
		/// <remarks>This object WILL change its identity once a BeginRequest record has been associated to it. This means
		/// <see cref="GetHashCode()"/> and <see cref="Equals(object b)"/> WILL behave differently after a call to 
		/// <see cref="SetBeginRequest(Record rec)"/>. This is not exposed in the public API.</remarks>
		internal Request (Socket s, ConcurrentDictionary<Socket, Request> requestsTable)
			: this(s)
		{
			if (requestsTable == null)
				throw new ArgumentNullException("requestsTable");

			this.RequestsTable = requestsTable;
		}
		*/
		#endregion
	}
}
