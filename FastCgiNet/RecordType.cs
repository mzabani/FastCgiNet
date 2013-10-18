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
}

