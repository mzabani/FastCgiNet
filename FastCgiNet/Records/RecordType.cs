using System;

namespace FastCgiNet
{
	public enum RecordType
	{
		FCGIBeginRequest = 1,
		FCGIAbortRequest,
		FCGIEndRequest,
		FCGIParams,
		FCGIStdin,
		FCGIStdout,
		FCGIStderr,
		FCGIData,
		FCGIGetValues,
		FCGIGetValuesResult,
		FCGIUnknownType
	}

    internal static class ExtensionMethods
    {
        /// <summary>
        /// Returns true if this RecordType is either Stderr, Stdin, Stdout or Params.
        /// </summary>
        public static bool IsStreamType(this RecordType type)
        {
            return type == RecordType.FCGIStderr || type == RecordType.FCGIStdin || type == RecordType.FCGIStdout || type == RecordType.FCGIParams;
        }
    }
}

