using System;
using FastCgiNet;
using System.Linq;

namespace FastCgiNet.Tests
{
	public class StaticHelper
	{
		public static Type[] RecordClasses = typeof(RecordBase).Assembly.GetTypes()
														   .Where(t => typeof(RecordBase).IsAssignableFrom(t))
														   .ToArray();
	}
}

