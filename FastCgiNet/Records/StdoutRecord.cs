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
		
        // TODO: There is no way to create secondary storage stdout records through the API. It doesn't make a whole lot of sense to me why
        // someone would need this yet.. think about it later

        internal StdoutRecord (byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStdout, data, offset, length, out endOfRecord)
		{
		}
	}
}
