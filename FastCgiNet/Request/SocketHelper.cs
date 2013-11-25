using System;
using System.Linq;
using System.Net.Sockets;

namespace FastCgiNet
{
    internal class SocketHelper
    {
        private static readonly SocketError[] ConnectionClosedErrors = new SocketError[] { SocketError.Shutdown, SocketError.Interrupted, SocketError.ConnectionReset, SocketError.ConnectionAborted };

        /// <summary>
        /// Determines if the exception thrown is a <see cref="SocketException"/> meaning that the connection was closed by the other side.
        /// </summary>
        public static bool IsConnectionAbortedByTheOtherSide(Exception e)
        {
            var socketEx = e as SocketException;
            if (socketEx == null)
                return false;

            return ConnectionClosedErrors.Contains(socketEx.SocketErrorCode);
        }
    }
}

