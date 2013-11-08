using System;
using FastCgiNet.Logging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace FastCgiNet
{
	public delegate void ReceiveStdinRecord(Request req, StdinRecord record);
	public delegate void ReceiveStdoutRecord(Request req, StdoutRecord record);
	public delegate void ReceiveStderrRecord(Request req, StderrRecord record);
	public delegate void ReceiveBeginRequestRecord(Request req, BeginRequestRecord record);
	public delegate void ReceiveEndRequestRecord(Request req, EndRequestRecord record);
	public delegate void ReceiveParamsRecord(Request req, ParamsRecord record);

	/// <summary>
	/// This class manages FastCgi Requests and builds FastCgi Records that come through the socket for you, allowing you
	/// to easily consume complete Records without the hassle of building them.
	/// </summary>
	public class SocketReader
	{
		private Socket Socket;
		private ILogger Logger;

		/// <summary>
		/// Requests indexed by their sockets. Be aware that a socket may be open without an associated request (a null request).
		/// </summary>
		public ConcurrentDictionary<Socket, Request> OpenSockets { get; private set; }

		/// <summary>
		/// Associates a socket to a request for the moment.
		/// If there is a different request assigned to this socket, then it is overwritten with this new request.
		/// </summary>
		void AssociateSocketToRequest(Socket sock, Request req)
		{
			OpenSockets[sock] = req;
		}

		#region Events to receive records
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveBeginRequestRecord OnReceiveBeginRequestRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveEndRequestRecord OnReceiveEndRequestRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveParamsRecord OnReceiveParamsRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveStdinRecord OnReceiveStdinRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveStdoutRecord OnReceiveStdoutRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveStderrRecord OnReceiveStderrRecord = delegate {};
		#endregion

		private void Work()
		{
			byte[] buffer = new byte[8192];
			int selectMaximumTime = -1;
			
			var recFactory = new RecordFactory();
			
			while (true)
			{
				try
				{
					// We want Select to return when there is incoming socket data
					var socketsReadSet = new List<Socket>() { Socket };
					
					Socket.Select(socketsReadSet, null, null, selectMaximumTime);
					
					foreach (Socket sock in socketsReadSet)
					{
						int minimumNeeded;
						if (Logger != null)
							Logger.Info("Received data through socket {0}", sock.GetHashCode());
						
						// If there is no last incomplete record for this socket, then we need at least 8 bytes for 
						// the header of a first record. Otherwise, we need anything we can get
						RecordBase lastIncompleteRecord = null;
						Request request;
						if (OpenSockets.ContainsKey (sock))
						{
							request = OpenSockets[sock];
							
							if (request != null)
								lastIncompleteRecord = request.LastIncompleteRecord;
							else
							{
								if (Logger != null)
									request = new Request(sock, OpenSockets, Logger);
								else
									request = new Request(sock, OpenSockets);
								
								AssociateSocketToRequest(sock, request);
							}
							
							if (lastIncompleteRecord == null)
								minimumNeeded = 8;
							else
								minimumNeeded = 1;
						}
						else
						{
							if (Logger != null)
								request = new Request(sock, OpenSockets, Logger);
							else
								request = new Request(sock, OpenSockets);
							
							AssociateSocketToRequest(sock, request);
							minimumNeeded = 8;
						}
						
						// To avoid waiting forever if a request contains less than the necessary amount of bytes
						// and never sends anything else, we check only once if there are more than 8 bytes available,
						// and if there aren't then we continue the foreach loop to the next socket
						int availableBytes = sock.Available;
						if (availableBytes == 0)
						{
							if (Logger != null)
								Logger.Info("Remote socket connection closed for socket {0}. Closing socket and skipping to next Socket.", sock.GetHashCode());
							
							request.CloseSocket();
							continue;
						}
						else if (availableBytes < minimumNeeded)
						{
							if (Logger != null)
								Logger.Debug("Needed {0} bytes but only got {1}. Skipping to next Socket.", minimumNeeded, availableBytes);
							
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
									if (Logger != null)
										Logger.Debug("Not enough bytes ({0}) to create a new record. Skipping to next Socket.", bytesRead - bytesFed);
									
									break;
								}
								
								if (Logger != null)
									Logger.Debug("Creating new record with {0} bytes still to be fed.", bytesRead - bytesFed);
								
								//lastIncompleteRecord = new Record (buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
								lastIncompleteRecord = recFactory.CreateRecordFromHeader(buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
								
								if (lastIncompleteRecord.RecordType == RecordType.FCGIBeginRequest)
								{
									// Take this opportunity to feed this beginrequestRecord to the current request
									request.SetBeginRequest((BeginRequestRecord)lastIncompleteRecord);
								}
								
								if (lastByteOfRecord == -1)
								{
									if (Logger != null)
										Logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());
									
									request.LastIncompleteRecord = lastIncompleteRecord;
									break;
								}
							}
							else
							{
								if (Logger != null)
									Logger.Debug("Feeding bytes into last incomplete record for socket {0}", sock.GetHashCode());
								
								lastIncompleteRecord.FeedBytes(buffer, bytesFed, bytesRead - bytesFed, out lastByteOfRecord);
							}
							
							// If we fed all bytes, our record still needs more data that we still haven't received.
							// To give other sockets a chance, break from the loop.
							if (lastByteOfRecord == -1)
							{
								request.LastIncompleteRecord = lastIncompleteRecord;
								if (Logger != null)
									Logger.Debug("Record is still incomplete. Saving it as last incomplete record for socket {0}. Skipping to next Socket.", sock.GetHashCode());
								
								break;
							}
							
							if (Logger != null)
								Logger.Debug("Record for socket {0} is complete.", sock.GetHashCode());
							
							bytesFed = lastByteOfRecord + 1;
							
							// Run the signed events with the complete record
							// Catch application errors to avoid service disruption
							try
							{
								switch (lastIncompleteRecord.RecordType)
								{
									case RecordType.FCGIBeginRequest:
										OnReceiveBeginRequestRecord (request, (BeginRequestRecord)lastIncompleteRecord);
										break;

									case RecordType.FCGIEndRequest:
										OnReceiveEndRequestRecord (request, (EndRequestRecord)lastIncompleteRecord);
										break;
										
									case RecordType.FCGIParams:
										OnReceiveParamsRecord (request, (ParamsRecord)lastIncompleteRecord);
										break;
										
									case RecordType.FCGIStdin:
										OnReceiveStdinRecord (request, (StdinRecord)lastIncompleteRecord);
										break;

									case RecordType.FCGIStdout:
										OnReceiveStdoutRecord (request, (StdoutRecord)lastIncompleteRecord);
										break;

									case RecordType.FCGIStderr:
										OnReceiveStderrRecord (request, (StderrRecord)lastIncompleteRecord);
										break;
										
									default:
										throw new Exception ("A record of unknown type was received!");
								}
							}
							catch (Exception ex)
							{
								// Log and end request
								if (Logger != null)
									Logger.Error(ex, "Application error");
								
								request.CloseSocket();
								continue;
							}
							finally
							{
								lastIncompleteRecord = null;
								request.LastIncompleteRecord = null;
								if (Logger != null)
									Logger.Debug("Setting last incomplete record for socket {0} to null.", sock.GetHashCode());
							}
						}
					}
				}
				catch (ObjectDisposedException e)
				{
					if (Logger != null)
						Logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (SocketException e)
				{
					if (e.SocketErrorCode != SocketError.Shutdown)
						throw;
					
					if (Logger != null)
						Logger.Info("Some operation was attempted on a closed socket. Exception: {0}", e);
				}
				catch (Exception e)
				{
					if (Logger != null)
						Logger.Fatal(e, "Exception would end the data receiving loop. This is extremely bad. Please file a bug report.");
				}
			}
		}

		/// <summary>
		/// Non blocking method to start receiving data and building records.
		/// </summary>
		public void Start()
		{
			Console.WriteLine("started working not yet");
			System.Threading.Tasks.Task.Factory.StartNew(Work);
			Console.WriteLine("started working");
		}

		public SocketReader(Socket s)
		{
			if (s == null)
				throw new ArgumentNullException("s");

			Socket = s;
			OpenSockets = new ConcurrentDictionary<Socket, Request>();
			Logger = null;
		}
	}
}

