using System;
using System.Collections.Generic;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	/// <summary>
	/// This class represents a FastCgi request. It is only a common base class for real FastCgi requests running over a socket
    /// or a named pipe, or any other desired communication medium.
	/// </summary>
	public abstract class Request
	{
		public ushort RequestId { get; protected set; }

        #region Streams
        public abstract FastCgiStream ParamsStream { get; }

        /// <summary>
        /// Enumerates the parameters in <see cref="ParamsStream"/>.
        /// </summary>
        /// <remarks>Enumerating the parameters will advance <see cref="ParamsStream"/>.</remarks>
        public IEnumerable<NameValuePair> Params
        {
            get
            {
                using (var paramsEnumerator = new NvpEnumerator(ParamsStream, ParamsStream.Length))
                {
                    while (paramsEnumerator.MoveNext())
                        yield return ((IEnumerator<NameValuePair>)paramsEnumerator).Current;
                }
            }
        }
        public abstract FastCgiStream Stdin { get; }
        public abstract FastCgiStream Stdout { get; }
        public abstract FastCgiStream Stderr { get; }
        #endregion

        //TODO: AddReceivedRecord and SetBeginRequest?

        /// <summary>
        /// When a record is sent by the other side, use this method to update the streams in this request.
        /// </summary>
        public void AddReceivedRecord(RecordBase rec)
        {
            if (rec == null)
                throw new ArgumentNullException("rec");

            switch (rec.RecordType)
            {
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

		public override int GetHashCode()
		{
			// A request is uniquely identified by its requestid.
			return RequestId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var b = obj as SocketRequest;
			if (b == null)
				return false;

			return b.RequestId.Equals(this.RequestId);
		}
	}
}
