using System;

namespace FastCgiNet.Tests
{
	class ByteUtils
	{
		public static bool SegmentsEqual(ArraySegment<byte> a, ArraySegment<byte> b)
		{
			int i, j;
			for (i = a.Offset, j = b.Offset; i < a.Count && j < b.Count; ++i, ++j)
			{
				Console.WriteLine("Comparing offsets {0} and {1}, valued {2} and {3}", i, j, a.Array[i], b.Array[j]);
				if (a.Array[i] != b.Array[j])
					return false;
			}
			
			if (a.Count != b.Count)
				return false;
			
			return true;
		}
	}
}

