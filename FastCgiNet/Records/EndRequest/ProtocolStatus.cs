using System;

namespace FastCgiNet
{
	public enum ProtocolStatus
	{
		RequestComplete = 0,
		CantMpxConn,
		Overloaded,
		UnknownRole
	}
}
