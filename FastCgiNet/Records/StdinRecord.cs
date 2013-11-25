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
		
		internal StdinRecord (byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStdin, data, offset, length, out endOfRecord)
		{
		}
	}
}
