using System;

namespace FastCgiNet
{
	/// <summary>
	/// Implement this to be able to log internal errors.
	/// </summary>
	public interface ILogger
	{
		void Info(string msg, params object[] prms);
		void Debug(string msg, params object[] prms);
		void Debug(Exception e);
		void Error(Exception e);
		void Fatal(Exception e);
	}
}

