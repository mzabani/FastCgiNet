using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using FastCgiNet.Logging;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastCgiNet
{
	/// <summary>
	/// This class will provide you parts of a FastCgi capable webserver implementation.
	/// </summary>
	public class FastCgiWebServer : IDisposable
	{
		private ILogger logger;

		/// <summary>
		/// Set this to an <see cref="FastCgiNet.Logging.ILogger"/> to log usage information.
		/// </summary>
		public void SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			this.logger = logger;
		}

		/// <summary>
		/// Disposes of resources, including a logger if one has been set.
		/// </summary>
		public void Dispose()
		{
			if (logger != null)
			{
				var disposableLogger = logger as IDisposable;
				if (disposableLogger != null)
					disposableLogger.Dispose();
			}
		}

		public FastCgiWebServer ()
		{
		}
	}
}
