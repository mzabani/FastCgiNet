using System;

namespace FastCgiNet
{
    /// <summary>
    /// Indicates that there weren't enough bytes to create some FastCgi structure, such as a Record or a NameValuePair.
    /// This does not mean that all the bytes that form the structure were needed; it means only that the first few bytes from which
    /// basic information is extracted were not available.
    /// </summary>
	internal class InsufficientBytesException : Exception
	{
		public InsufficientBytesException (string msg)
			: base(msg)
		{
		}

		public InsufficientBytesException ()
			: base()
		{
			
		}
	}
}

