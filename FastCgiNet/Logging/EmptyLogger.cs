using System;

namespace FastCgiNet.Logging
{
	class EmptyLogger : ILogger
	{
		public void Info (string msg, params object[] prms)
		{
		}
		public void Debug (string msg, params object[] prms)
		{
		}
		public void Debug (Exception e)
		{
		}
		public void Error (Exception e)
		{
		}
		public void Error (Exception e, string msg, params object[] prms)
		{
		}
		public void Fatal (Exception e)
		{
		}
		public void Fatal (Exception e, string msg, params object[] prms)
		{
		}
	}
}

