using System;

namespace FastCgiNet
{
	public enum Role : short
	{
		Responder = 1,
		Authorizer,
		Filter
	}
}

