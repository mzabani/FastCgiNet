using System;

namespace FastCgiNet
{
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

