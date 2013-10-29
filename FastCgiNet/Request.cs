using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using FastCgiNet.Logging;

namespace FastCgiNet
{
	/// <summary>
	/// This class identifies a FastCgi request uniquely in time.
	/// </summary>
	public class Request
	{
		public ushort RequestId { get; private set; }
		internal Socket Socket { get; private set; }
		internal Record LastIncompleteRecord;
		private ILogger Logger;

		private ConcurrentDictionary<Socket, Request> RequestsTable;

		/// <summary>
		/// Sends a record over the wire. This method does not throw if the socket has been closed.
		/// </summary>
		/// <returns>True if the record was sent successfuly, false if the socket was closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but the record couldn't be sent for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>
		public bool Send(Record rec)
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
				//TODO: Better logging, i.e. identify Request
				if (Logger != null)
					Logger.Info("Could not send data through socket because it was closed");

				return false;
			}
			catch (SocketException e)
			{
				//TODO: Better logging, i.e. identify Request
				if (Logger != null)
					Logger.Info("Could not send data through socket because it was closed");

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
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException e)
			{
				//TODO: Better logging, i.e. identify Request
				if (Logger != null)
					Logger.Info("Could not close socket because it was already closed");

				if (e.SocketErrorCode != SocketError.Shutdown)
					throw;
			}

			if (RequestsTable != null)
			{
				Request trash;
				bool elementRemoved = RequestsTable.TryRemove(Socket, out trash);

				if (elementRemoved == false && connectionClosedError == false)
				{
					//TODO: Better logging, i.e. identify Request
					if (Logger != null)
						Logger.Info("Could not close socket because it was already closed");

					connectionClosedError = true;
				}
			}

			return !connectionClosedError;
		}

		/// <summary>
		/// Sets some basic properties of this request such as its <see cref="RequestId"/>.
		/// </summary>
		/// <param name="rec">The BeginRequest record.</param> 
		public void SetBeginRequest(Record rec)
		{
			if (rec == null)
				throw new ArgumentNullException("rec");
			else if (rec.RecordType != RecordType.FCGIBeginRequest)
				throw new ArgumentException("The record has to be of type BeginRequest");

			RequestId = rec.RequestId;
		}

		public override int GetHashCode ()
		{
			// A request is uniquely identified by its socket.
			// DO NOT involve RequestId in this, because it starts with 0 and then may change.
			return Socket.GetHashCode();
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var b = obj as Request;
			if (b == null)
				return false;

			return b.Socket.Equals(this.Socket);
		}

		public Request(Socket s)
		{
			if (s == null)
				throw new ArgumentNullException("s");

			this.Socket = s;
		}

		public Request (Socket s, ILogger logger)
			: this(s)
		{
			if (logger == null)
				throw new ArgumentNullException("Logger is null. Use another constructor if you don't want to set a logger");

			Logger = logger;
		}

		/// <summary>
		/// This constructor helps maintain a table of requests.
		/// </summary>
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
		internal Request (Socket s, ConcurrentDictionary<Socket, Request> requestsTable)
			: this(s)
		{
			if (requestsTable == null)
				throw new ArgumentNullException("requestsTable");

			this.RequestsTable = requestsTable;
		}
	}
}
