using System;
using System.Collections.Generic;
using FastCgiNet.Streams;

namespace FastCgiNet.Requests
{
	/// <summary>
	/// This class represents a FastCgi request. It serves as a common base class for real FastCgi requests running over a socket
    /// or a named pipe, or any other desired communication medium.
	/// </summary>
	public abstract class FastCgiRequest : IDisposable
	{
		public ushort RequestId { get; protected set; }

        /// <summary>
        /// This request's Record Factory.
        /// </summary>
        protected readonly RecordFactory RecordFactory;

        /// <summary>
        /// When data sent by the other side is received, feed it to this request using this method. This will append data to the streams
        /// or set this request's properties. It is important that bytes are fed sequentially to this method.
        /// </summary>
        /// <param name="data">The array containing the received data.</param>
        /// <param name="offset">The offset from which data is to be found in the <paramref name="data"/> array.</param>
        /// <param name="count">The number of bytes to be read from the <paramref name="data"/> array.</param>
        /// <returns>The records built with the newly received data.</returns>
        public IEnumerable<RecordBase> FeedBytes(byte[] data, int offset, int count)
        {
            foreach (var rec in RecordFactory.Read(data, offset, count))
            {
                AddReceivedRecord(rec);
                yield return rec;
            }
        }

        protected bool BeginRequestSent { get; private set; }
        protected bool EndRequestSent { get; private set; }
        /// <summary>
        /// Whenever you want to send a record to the other side, call this base method to do some book keeping for you before you
        /// actually send the record.
        /// </summary>
        /// <param name="rec">The record to send.</param>
        protected virtual void Send(RecordBase rec)
        {
            if (rec == null)
                throw new ArgumentNullException("rec");

            if (rec.RecordType != RecordType.FCGIBeginRequest)
            {
                if (rec.RequestId != RequestId)
                    throw new ArgumentException("The record's RequestId is different from this Request's");
            }
            else
            {
                if (BeginRequestSent)
                    throw new InvalidOperationException("A BeginRequest has already been sent for this Request");

                BeginRequestSent = true;
            }

            if (rec.RecordType == RecordType.FCGIEndRequest)
            {
                if (EndRequestSent)
                    throw new InvalidOperationException("An EndRequest has already been sent for this Request");

                EndRequestSent = true;
            }
        }

        #region Streams
        public abstract FastCgiStream Data { get; }
        public abstract FastCgiStream Params { get; }
        public abstract FastCgiStream Stdin { get; }
        public abstract FastCgiStream Stdout { get; }
        public abstract FastCgiStream Stderr { get; }
        #endregion

        public virtual void Dispose()
        {
            Params.Dispose();
            Stdin.Dispose();
            Stdout.Dispose();
            Stderr.Dispose();
            RecordFactory.Dispose();
        }

        protected bool BeginRequestReceived { get; private set; }
        protected bool EndRequestReceived { get; private set; }
        /// <summary>
        /// This method is called internally when data is received and fed to this Request. It basically sets this request's properties
        /// or appends data to the streams. Override it and call the base method itself to implement your own logic and checking.
        /// </summary>
        protected virtual void AddReceivedRecord(RecordBase rec)
        {
            if (rec == null)
                throw new ArgumentNullException("rec");

            switch (rec.RecordType)
            {
                case RecordType.FCGIBeginRequest:
                    // Make sure we are not getting a BeginRequest once again, as this could be serious.
                    if (BeginRequestReceived)
                        throw new InvalidOperationException("A BeginRequest Record has already been received by this Request");

                    BeginRequestReceived = true;
                    RequestId = ((BeginRequestRecord)rec).RequestId;
                    break;

                case RecordType.FCGIEndRequest:
                    // Make sure we are not getting a BeginRequest once again, as this could be serious.
                    if (EndRequestReceived)
                        throw new InvalidOperationException("An EndRequest Record has already been received by this Request");
                    
                    EndRequestReceived = true;
                    break;

                case RecordType.FCGIParams:
                    Params.AppendStream(((StreamRecordBase)rec).Contents);
                    break;
                case RecordType.FCGIStdin:
                    Stdin.AppendStream(((StreamRecordBase)rec).Contents);
                    break;
                case RecordType.FCGIStdout:
                    Stdout.AppendStream(((StreamRecordBase)rec).Contents);
                    break;
                case RecordType.FCGIStderr:
                    Stderr.AppendStream(((StreamRecordBase)rec).Contents);
                    break;
            }
        }

		public override int GetHashCode()
		{
			// A request is uniquely identified by its requestid.
			return RequestId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var b = obj as ApplicationSocketRequest;
			if (b == null)
				return false;

			return b.RequestId.Equals(this.RequestId);
		}

        /// <summary>
        /// Initializes a FastCgi Request that stores all records' contents in memory.
        /// </summary>
        [Obsolete("Use the constructor that needs a RecordFactory")]
        public FastCgiRequest()
        {
            RecordFactory = new RecordFactory();
            BeginRequestSent = false;
            BeginRequestReceived = false;
            EndRequestSent = false;
            EndRequestReceived = false;
        }

        /// <summary>
        /// Builds a FastCgi Request that uses the supplied <paramref name="recordFactory"/> to build the records
        /// that represent the incoming data.
        /// </summary>
        /// <param name="recordFactory">
        /// The factory used to create records. This object's life cycle is controlled by this request.
        /// That means that when this request is disposed, so will this record factory be.
        /// </param>
        public FastCgiRequest(RecordFactory recordFactory)
        {
            if (recordFactory == null)
                throw new ArgumentNullException("recordFactory");

            RecordFactory = recordFactory;
            BeginRequestSent = false;
            BeginRequestReceived = false;
            EndRequestSent = false;
            EndRequestReceived = false;
        }
	}
}
