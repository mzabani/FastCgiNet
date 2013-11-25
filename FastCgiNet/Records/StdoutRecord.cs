using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class StdoutRecord : StreamRecordBase, IDisposable
	{
		/// <summary>
		/// Initializes a new Stdout FastCgi record. You should not rely on this record's ContentLength, PaddingLength and EmptyContentData properties until
		/// you call <see cref="GetBytes()"/>, which will calculate and set them for you.
		/// </summary>
		/// <param name="requestId">The RequestId of this record.</param>
		public StdoutRecord(ushort requestId)
			: base(RecordType.FCGIStdout, requestId)
		{
		}
		
		internal StdoutRecord (byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStdout, data, offset, length, out endOfRecord)
		{
		}
	}
}
