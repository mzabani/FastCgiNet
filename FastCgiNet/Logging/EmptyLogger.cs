using System;

namespace FastCgiNet
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
		public void Fatal (Exception e)
		{
		}
	}
}

