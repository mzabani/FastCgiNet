using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	public class StderrRecord : StreamRecordBase, IDisposable
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

        // TODO: There is no way to create secondary storage stderr records through the API. It doesn't make a whole lot of sense to me why
        // someone would need this yet.. think about it later
		
		internal StderrRecord (byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIStderr, null, data, offset, length, out endOfRecord)
		{
		}
	}
}
