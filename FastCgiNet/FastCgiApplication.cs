using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using FastCgiNet.Logging;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastCgiNet
{
	public delegate void ReceiveRecord(Request req, Record record);

	/// <summary>
	/// This class will provide you a FastCgi hosting application. It will listen and deal with connections while
	/// providing ways for you to know what records different FastCgi connections are receiving.
	/// </summary>
	public class FastCgiApplication : IDisposable
	{
		private const int listenBacklog = 500;
		private Socket tcpListenSocket;
		private Socket unixListenSocket;
		private bool SomeListenSocketHasBeenBound
		{
			get
			{
				return tcpListenSocket.IsBound || unixListenSocket.IsBound;
			}
		}
		private ILogger logger;

		public bool IsRunning { get; private set; }

		/// <summary>
		/// Requests indexed by their sockets.
		/// </summary>
		ConcurrentDictionary<Socket, Request> openSockets = new ConcurrentDictionary<Socket, Request>();

		#region Events to receive records
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveRecord OnReceiveBeginRequestRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveRecord OnReceiveParamsRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveRecord OnReceiveStdinRecord = delegate {};
		#endregion

		/// <summary>
		/// Associates a socket to a request for the moment.
		/// If there is a different request assigned to this socket, then it is overwritten with this new request.
		/// </summary>
		void AssociateSocketToRequest(Socket sock, Request req)
		{
			openSockets[sock] = req;
		}

		void ReceiveConnectionData()
		{
			byte[] buffer = new byte[8192];
			int selectMaximumTime = 10000;

			while (true)
			{
				try
				{
					List<Socket> connectedSockets = openSockets.Keys.ToList();
					if (connectedSockets.Count > 0)
					{
						if (logger != null)
							logger.Debug("+ Connected Sockets: {0}", connectedSockets.Count);

						Socket.Select (connectedSockets, null, null, selectMaximumTime);
			
						foreach (Socket sock in connectedSockets)
						{
							int minimumNeeded;
							if (logger != null)
								logger.Info("Received connection: Socket {0}", sock.GetHashCode());
			
							// If there is no last incomplete record for this socket, then we need at least 8 bytes for 
							// the header of a first record. Otherwise, we need anything we can get
							Record lastIncompleteRecord = null;
							Request request;
							if (openSockets.ContainsKey (sock))
							{
								request = openSockets[sock];

								if (request != null)
									lastIncompleteRecord = request.LastIncompleteRecord;
								else
								{
									if (logger != null)
										request = new Request(sock, openSockets, logger);
									else
										request = new Request(sock, openSockets);

									AssociateSocketToRequest(sock, request);
								}

								if (lastIncompleteRecord == null)
									minimumNeeded = 8;
								else
									minimumNeeded = 1;
							}
							else
							{
								if (logger != null)
									request = new Request(sock, openSockets, logger);
								else
									request = new Request(sock, openSockets);

								AssociateSocketToRequest(sock, request);
								minimumNeeded = 8;
							}
			
							// To avoid waiting forever if a request contains less than the necessary amount of bytes
							// and never sends anything else, we check only once if there are more than 8 bytes available,
							// and if there aren't then we continue the foreach loop to the next socket
							int availableBytes = sock.Available;
							if (availableBytes == 0)
							{
								if (logger != null)
									logger.Info("Remote socket connection closed for socket {0}. Closing socket and skipping to next Socket.", sock.GetHashCode());

								request.CloseSocket();
								continue;
							}
							else if (availableBytes < minimumNeeded)
							{
								if (logger != null)
									logger.Debug("Needed {0} bytes but only got {1}. Skipping to next Socket.", minimumNeeded, availableBytes);

								continue;
							}

							int bytesRead = sock.Receive(buffer, 0, buffer.Length, SocketFlags.None);
							
							// Feed every byte read into records
							int bytesFed = 0;
							while (bytesFed < bytesRead)
							{
								int lastByteOfRecord;
								
								// Are we going to create a new record or feed the last incomplete one?
								if (lastIncompleteRecord == null)
								{
									// There is a possibility of there not being 8 bytes available at this point..
									// If so, we have to wait until the socket has at least 8 bytes available. 
									// Skip to next socket by breaking out of the loop
									if (bytesRead - bytesFed < 8)
									{
										if (logger != null)
											logger.Debug("Not enough bytes ({0}) to create a new record. Skipping to next Socket.", bytesRead - bytesFed);

										break;
									}

									if (logger != null)
										logger.Debug("Creating new record with {0} bytes still to be fed.", bytesRead - bytesFed);

									lastIncompleteRecord = new Record (buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
									
									if (lastIncompleteRecord.RecordType == RecordType.FCGIBeginRequest)
									{
										// Take this opportunity to feed this beginrequest to the current request
										request.SetBeginRequest(lastIncompleteRecord);
									}
			
									if (lastByteOfRecord == -1)
									{
										if (logger != null)
											logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());

										request.LastIncompleteRecord = lastIncompleteRecord;
										break;
									}
								}
								else
								{
									if (logger != null)
										logger.Debug("Feeding bytes into last incomplete record for socket {0}", sock.GetHashCode());

									lastIncompleteRecord.FeedBytes (buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
								}
								
								// If we fed all bytes, our record still needs more data that we still haven't received.
								// To give other sockets a chance, break from the loop.
								if (lastByteOfRecord == -1)
								{
									request.LastIncompleteRecord = lastIncompleteRecord;
									if (logger != null)
										logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());

									break;
								}

								if (logger != null)
									logger.Debug("Record for socket {0} is complete.", sock.GetHashCode());

								bytesFed = lastByteOfRecord + 1;
								
								// Run the signed events with the complete record
								// Catch application errors to avoid service disruption
								try
								{
									switch (lastIncompleteRecord.RecordType)
									{
										case RecordType.FCGIBeginRequest:
											OnReceiveBeginRequestRecord (request, lastIncompleteRecord);
											break;
										
										case RecordType.FCGIParams:
											OnReceiveParamsRecord (request, lastIncompleteRecord);
											break;
										
										case RecordType.FCGIStdin:
											OnReceiveStdinRecord (request, lastIncompleteRecord);
											break;
										
										default:
											throw new Exception ("A record of unknown type was received!");
									}
								}
								catch (Exception ex)
								{
									// Log and end request
									if (logger != null)
										logger.Error(ex, "Application error");

									request.CloseSocket();
									continue;
								}
								finally
								{
									lastIncompleteRecord = null;
									request.LastIncompleteRecord = null;
									if (logger != null)
										logger.Debug("Setting last incomplete record for socket {0} to null.", sock.GetHashCode());
								}
							}
						}
					}
					else
					{
						//TODO: Think of something better..
						System.Threading.Thread.Sleep (100);
					}
				}
				catch (ObjectDisposedException e)
				{
					if (logger != null)
						logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (SocketException e)
				{
					if (e.SocketErrorCode != SocketError.Shutdown)
						throw;

					if (logger != null)
						logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (Exception e)
				{
					if (logger != null)
						logger.Fatal(e, "Exception would end the data receiving loop. This is extremely bad. Please file a bug report.");
				}
			}
		}

		void AcceptConnection(IAsyncResult ar)
		{
			try
			{
				Socket newConnectionSocket = tcpListenSocket.EndAccept(ar);
				AssociateSocketToRequest(newConnectionSocket, null);
			}
			catch (Exception e)
			{
				if (logger != null)
					logger.Error(e);
			}
		}

		void WaitForConnections()
		{
			int pollTime_us = 10000;
			while (IsRunning)
			{
				// This is very ugly code.. lots of room for improvement

				if (tcpListenSocket.LocalEndPoint != null && tcpListenSocket.Poll(pollTime_us / 2, SelectMode.SelectRead))
				{
					try {
						tcpListenSocket.BeginAccept(AcceptConnection, null);
					} catch (SocketException) {
						//TODO: Look for the code for a "no connection pending" error at the Windows Sockets version 2 API error code documentation,
						// and continue only in that case..
						continue;
					}
				}

				if (unixListenSocket.LocalEndPoint != null && unixListenSocket.Poll(pollTime_us / 2, SelectMode.SelectRead))
				{
					//TODO: What is the handshake when accepting unix sockets? Maybe it is worth it doing this synchronously
					try {
						unixListenSocket.BeginAccept(AcceptConnection, null);
					} catch (SocketException) {
						//TODO: Look for possible error codes that we can ignore
						continue;
					}
				}
			}
		}

		/// <summary>
		/// Set this to an <see cref="FastCgiNet.Logging.ILogger"/> to log usage information.
		/// </summary>
		public void SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			this.logger = logger;
		}

		/// <summary>
		/// Defines on what address and what port the TCP socket will listen on.
		/// </summary>
		public void Bind(IPAddress addr, int port)
		{
			tcpListenSocket.Bind(new IPEndPoint(addr, port));
		}

#if __MonoCS__
		/// <summary>
		/// Defines the unix socket path to listen on.
		/// </summary>
		public void Bind(string socketPath)
		{
			var endpoint = new Mono.Unix.UnixEndPoint(socketPath);
			unixListenSocket.Bind (endpoint);
		}
#endif

		/// <summary>
		/// Start this FastCgi application. This method blocks while the program runs.
		/// </summary>
		public void Start() {
			if (!SomeListenSocketHasBeenBound)
				throw new InvalidOperationException("You have to bind to some address or unix socket file first");

			if (tcpListenSocket.IsBound)
				tcpListenSocket.Listen(listenBacklog);
			if (unixListenSocket.IsBound)
				unixListenSocket.Listen(listenBacklog);

			// Wait for connections without blocking
			Task.Factory.StartNew(WaitForConnections);

			IsRunning = true;

			// Block here
			ReceiveConnectionData();
		}

		/// <summary>
		/// Start this FastCgi application. This method does not block and only returns when the server is ready to accept connections.
		/// </summary>
		public void StartInBackground() {
			if (!SomeListenSocketHasBeenBound)
				throw new InvalidOperationException("You have to bind to some address or unix socket file first");

			if (tcpListenSocket.IsBound)
				tcpListenSocket.Listen(listenBacklog);
			if (unixListenSocket.IsBound)
				unixListenSocket.Listen(listenBacklog);

			// Set this before waiting for connections
			IsRunning = true;

			//TODO: If one of the tasks below is delayed (why in the world would that happen, idk) then this
			// method returns without being ready to accept connections..
			Task.Factory.StartNew(ReceiveConnectionData);
			Task.Factory.StartNew(WaitForConnections);
		}

		/// <summary>
		/// Closes the listen socket and all active connections abruptely.
		/// </summary>
		public void Stop()
		{
			IsRunning = false;

			if (tcpListenSocket != null && tcpListenSocket.IsBound)
				tcpListenSocket.Close();
			if (unixListenSocket != null && unixListenSocket.IsBound)
				unixListenSocket.Close();

			//TODO: Stop task that waits for connection data..

			foreach (var socketAndRequest in openSockets)
			{
				socketAndRequest.Key.Close();
			}
		}

		/// <summary>
		/// Stops the server if it hasn't been stopped and disposes of resources, including a logger if one has been set.
		/// </summary>
		public void Dispose()
		{
			Stop();

			if (tcpListenSocket != null)
				tcpListenSocket.Dispose();
			if (unixListenSocket != null)
				unixListenSocket.Dispose();

			if (logger != null)
			{
				var disposableLogger = logger as IDisposable;
				if (disposableLogger != null)
					disposableLogger.Dispose();
			}
		}

		public FastCgiApplication ()
		{
			tcpListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			unixListenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP); // man unix (7) says most unix implementations are safe on deliveryand order
			//TODO: ipv6
			IsRunning = false;
		}
	}
}
