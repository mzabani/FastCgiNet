using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	public class ParamsRecord : StreamRecordBase
	{
		/// <summary>
		/// Do not write directly to this stream unless you know what you're doing! Use the <see cref="Add(NameValuePair nvp)"/> or <see cref="Add(string key, string value)"/> methods,
		/// which will then write to the stream correctly.
		/// </summary>
		public override RecordContentsStream Contents
		{
			get
			{
				return base.Contents;
			}
			internal set
			{
				base.Contents = value;
			}
		}

		public ParamsRecord(ushort requestId)
			: base(RecordType.FCGIParams,  requestId)
		{
		}

        // TODO: There is no way to create secondary storage params records through the API. It doesn't make a whole lot of sense to me why
        // someone would need this yet.. think about it later

		internal ParamsRecord(byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIParams, data, offset, length, out endOfRecord)
		{
		}
	}
}
