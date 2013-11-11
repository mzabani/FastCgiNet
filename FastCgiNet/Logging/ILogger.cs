using System;

namespace FastCgiNet.Logging
{
	/// <summary>
	/// Implement this to be able to log internal errors and debug messages, among others.
	/// </summary>
	public interface ILogger
	{
		void Info(string msg, params object[] prms);
		void Debug(string msg, params object[] prms);
		void Debug(Exception e);
		void Error(Exception e);
		void Error(Exception e, string msg, params object[] prms);
		void Fatal(Exception e);
		void Fatal(Exception e, string msg, params object[] prms);
	}
}
