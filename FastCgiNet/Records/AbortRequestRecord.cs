using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
    public class AbortRequestRecord : RecordBase
	{
        public override IEnumerable<ArraySegment<byte>> GetBytes()
        {
            // An AbortRequest Record is just a header.
            yield return new ArraySegment<byte>(Header);
        }

		internal override void FeedBytes(byte[] data, int offset, int length, out int endOfRecord)
		{
            throw new InvalidOperationException("This AbortRequest Record is complete");
		}

        public AbortRequestRecord(ushort requestId)
			: base(RecordType.FCGIAbortRequest, requestId)
		{
		}

        internal AbortRequestRecord(byte[] data, int offset, int length, out int endOfRecord)
			: base(data, offset, length, RecordType.FCGIAbortRequest)
		{
            endOfRecord = offset + 8 - 1;
		}
	}
}
