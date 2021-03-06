using FastCgiNet.Streams;
using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class DataRecord : StreamRecordBase, IDisposable
	{
		/// <summary>
		/// Initializes a new Data FastCgi record. You should not rely on this record's ContentLength, PaddingLength and EmptyContentData properties until
		/// you call <see cref="GetBytes()"/>, which will calculate and set them for you.
		/// </summary>
		/// <param name="requestId">The RequestId of this record.</param>
        public DataRecord(ushort requestId)
			: base(RecordType.FCGIData, requestId)
		{
		}
		
        /// <summary>
        /// Initializes a new Data FastCgi record whose contents will be stored in secondary storage. You should not rely on this
        /// record's ContentLength, PaddingLength and EmptyContentData properties until you call <see cref="GetBytes()"/>,
        /// which will calculate and set them for you.
        /// </summary>
        /// <param name="requestId">The RequestId of this record.</param>
        public DataRecord(ushort requestId, Stream secondaryStorageStream)
            : base(RecordType.FCGIData, requestId, secondaryStorageStream)
        {
        }

        internal DataRecord (byte[] data, int offset, int length, out int endOfRecord)
            : base(RecordType.FCGIData, data, offset, length, out endOfRecord)
		{
		}
	}
}
