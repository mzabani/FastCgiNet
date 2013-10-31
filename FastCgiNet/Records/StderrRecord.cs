using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class StderrRecord : StreamRecord, IDisposable
	{
		/// <summary>
		/// Initializes a new Stderr FastCgi record. You should not rely on this record's ContentLength, PaddingLength and EmptyContentData properties until
		/// you call <see cref="GetBytes()"/>, which will calculate and set them for you.
		/// </summary>
		/// <param name="requestId">The RequestId of this record.</param>
		public StderrRecord(ushort requestId)
			: base(RecordType.FCGIStderr, requestId)
		{
		}
		
		internal StderrRecord (byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStderr, data, offset, length, out endOfRecord)
		{
		}
	}
}
