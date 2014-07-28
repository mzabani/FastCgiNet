using FastCgiNet.Streams;
using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class StdinRecord : StreamRecordBase, IDisposable
	{
		/// <summary>
		/// Initializes a new Stdin FastCgi record. You should not rely on this record's ContentLength, PaddingLength and EmptyContentData properties until
		/// you call <see cref="GetBytes()"/>, which will calculate and set them for you.
		/// </summary>
		/// <param name="requestId">The RequestId of this record.</param>
		public StdinRecord(ushort requestId)
			: base(RecordType.FCGIStdin, requestId)
		{
		}

        /// <summary>
        /// Initializes a new Stdin FastCgi record whose contents will be stored in secondary storage. You should not rely on this
        /// record's ContentLength, PaddingLength and EmptyContentData properties until you call <see cref="GetBytes()"/>,
        /// which will calculate and set them for you.
        /// </summary>
        /// <param name="requestId">The RequestId of this record.</param>
        public StdinRecord(ushort requestId, ISecondaryStorageOps secondaryStorageOps)
            : base(RecordType.FCGIStdin, requestId, secondaryStorageOps)
        {
        }
		
        internal StdinRecord (byte[] data, ISecondaryStorageOps secondaryStorageOps, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStdin, secondaryStorageOps, data, offset, length, out endOfRecord)
		{
		}
	}
}
