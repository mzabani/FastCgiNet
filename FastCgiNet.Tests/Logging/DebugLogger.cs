using System;
using FastCgiNet.Logging;

namespace FastCgiNet.Logging
{
	/// <summary>
	/// This logger prints Debug and Info messages to Stderr. Everything else is simply ignored.
	/// </summary>
	public class DebugLogger : ILogger
	{
		public void Info (string msg, params object[] prms)
		{
			Console.Error.WriteLine(msg, prms);
		}
		public void Debug (string msg, params object[] prms)
		{
			Console.Error.WriteLine(msg, prms);
		}
		public void Debug (Exception e)
		{
			Console.Error.WriteLine(e.ToString());
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
