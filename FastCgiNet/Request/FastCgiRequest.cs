using System;
using System.Collections.Generic;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	/// <summary>
	/// This class represents a FastCgi request. It serves as a common base class for real FastCgi requests running over a socket
    /// or a named pipe, or any other desired communication medium.
	/// </summary>
	public abstract class FastCgiRequest : IDisposable
	{
		public ushort RequestId { get; protected set; }
        public Role Role { get; protected set; }
        public bool ApplicationMustCloseConnection { get; protected set; }

        #region Streams
        public abstract FastCgiStream ParamsStream { get; }
        public abstract FastCgiStream Stdin { get; }
        public abstract FastCgiStream Stdout { get; }
        public abstract FastCgiStream Stderr { get; }
        #endregion

        public virtual void Dispose()
        {
            ParamsStream.Dispose();
            Stdin.Dispose();
            Stdout.Dispose();
            Stderr.Dispose();
        }

        /// <summary>
        /// When a record is sent by the other side, use this method to update the streams in this request.
        /// </summary>
        public virtual void AddReceivedRecord(RecordBase rec)
        {
            if (rec == null)
                throw new ArgumentNullException("rec");

            switch (rec.RecordType)
            {
                case RecordType.FCGIBeginRequest:
                    RequestId = ((BeginRequestRecord)rec).RequestId;
                    Role  = ((BeginRequestRecord)rec).Role;
                    ApplicationMustCloseConnection = ((BeginRequestRecord)rec).ApplicationMustCloseConnection;
                    break;

                case RecordType.FCGIParams:
                    ParamsStream.AppendStream(((StreamRecordBase)rec).Contents);
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
	}
}
