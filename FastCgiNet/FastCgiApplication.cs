using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
		const int listenBacklog = 500;
		bool waitForConnections;
		Socket tcpListenSocket;
		ILogger logger;

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
						logger.Debug("+ Connected Sockets: {0}", connectedSockets.Count);

						Socket.Select (connectedSockets, null, null, selectMaximumTime);
			
						foreach (Socket sock in connectedSockets)
						{
							int minimumNeeded;
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
									request = new Request(sock, openSockets, logger);
									AssociateSocketToRequest(sock, request);
								}

								if (lastIncompleteRecord == null)
									minimumNeeded = 8;
								else
									minimumNeeded = 1;
							}
							else
							{
								request = new Request(sock, openSockets, logger);
								AssociateSocketToRequest(sock, request);
								minimumNeeded = 8;
							}
			
							// To avoid waiting forever if a request contains less than the necessary amount of bytes
							// and never sends anything else, we check only once if there are more than 8 bytes available,
							// and if there aren't then we continue the foreach loop to the next socket
							int availableBytes = sock.Available;
							if (availableBytes == 0)
							{
								//TODO: Remote connection shutdown, Log and ignore?
								logger.Info("Remote socket connection closed for socket {0}. Closing socket and skipping to next Socket.", sock.GetHashCode());
								request.CloseSocket();
								continue;
							}
							else if (availableBytes < minimumNeeded)
							{
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
										logger.Debug("Not enough bytes ({0}) to create a new record. Skipping to next Socket.", bytesRead - bytesFed);
										break;
									}

									logger.Debug("Creating new record with {0} bytes still to be fed.", bytesRead - bytesFed);
									lastIncompleteRecord = new Record (buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
									
									if (lastIncompleteRecord.RecordType == RecordType.FCGIBeginRequest)
									{
										// Take this opportunity to feed this beginrequest to the current request
										request.SetBeginRequest(lastIncompleteRecord);
									}
			
									if (lastByteOfRecord == -1)
									{
										logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());
										request.LastIncompleteRecord = lastIncompleteRecord;
										break;
									}
								}
								else
								{
									logger.Debug("Feeding bytes into last incomplete record for socket {0}", sock.GetHashCode());
									lastIncompleteRecord.FeedBytes (buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
								}
								
								// If we fed all bytes, our record still needs more data that we still haven't received.
								// To give other sockets a chance, break from the loop.
								if (lastByteOfRecord == -1)
								{
									request.LastIncompleteRecord = lastIncompleteRecord;
									logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());
									break;
								}

								logger.Debug("Record for socket {0} is complete.", sock.GetHashCode());

								bytesFed = lastByteOfRecord + 1;
								
								// Run the signed events with the complete record
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
									//TODO: Log and end request?
								}
								finally
								{
									lastIncompleteRecord = null;
									request.LastIncompleteRecord = null;
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
					logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (SocketException e)
				{
					if (e.SocketErrorCode != SocketError.Shutdown)
						throw;
					
					logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (Exception e)
				{
					logger.Fatal(e);
					throw;
				}
			}
			
		}

		void WaitForConnections()
		{
			int pollTime_us = 10000;
			while (waitForConnections)
			{
				if (tcpListenSocket.Poll(pollTime_us, SelectMode.SelectRead))
				{
					Socket newConnectionSocket = null;
					try {
						newConnectionSocket = tcpListenSocket.Accept();
					} catch (SocketException) {
						//TODO: Look for the code for a "no connection pending" error at the Windows Sockets version 2 API error code documentation,
						// and continue only in that case..
						continue;
					}

					AssociateSocketToRequest(newConnectionSocket, null);
				}
			}
		}

		/// <summary>
		/// Set this to an <see cref="ILogger"/> to log usage information.
		/// </summary>
		public void SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			this.logger = logger;
		}

		/// <summary>
		/// Defines on what address and what port the TCP listen socket will listen on.
		/// </summary>
		public void Bind(IPAddress addr, int port)
		{
			tcpListenSocket.Bind(new IPEndPoint(addr, port));
		}

		/// <summary>
		/// Start this FastCgi application. This method blocks while the program runs.
		/// </summary>
		public void Start() {
			tcpListenSocket.Listen(listenBacklog);

			// Wait for connections without blocking
			Task.Factory.StartNew(WaitForConnections);

			// Block here
			ReceiveConnectionData();
		}

		public void Dispose()
		{
			tcpListenSocket.Close();
			foreach (var socketAndRequest in openSockets)
			{
				socketAndRequest.Key.Close();
			}
		}

		public FastCgiApplication ()
		{
			tcpListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//TODO: ipv6 and unix sockets
			waitForConnections = true;

			logger = new EmptyLogger();
		}
	}
}
