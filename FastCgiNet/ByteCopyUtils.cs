using System;

namespace FastCgiNet
{
	class ByteCopyUtils
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
	}
}

