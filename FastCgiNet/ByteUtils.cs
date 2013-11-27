using System;
using System.Linq;

namespace FastCgiNet
{
	internal class ByteUtils
	{
		/// <summary>
		/// Verifies if going from position <paramref name="offset"/> for <paramref name="length"/> bytes would 
		/// go over the bounds of the array <paramref name="buffer"/>. Also verifies if <paramref name="offset"/> is negative.
		/// </summary>
		/// <returns><c>true</c>, if the operation would be safe, <c>false</c> otherwise.</returns>
		public static bool CheckArrayBounds(byte[] buffer, int offset, int length)
		{
			if (offset + length > buffer.Length)
				return false;
			else if (offset < 0)
				return false;

			return true;
		}

		public static bool AreEqual(byte[] a, byte[] b)
		{
			//TODO: Unsafe code with memory-aligned reads would be much faster!
			if (a == null || b == null)
				throw new ArgumentNullException("Both arrays have to be not null");

			return a.Length == b.Length && a.SequenceEqual(b);
		}
	}
}
