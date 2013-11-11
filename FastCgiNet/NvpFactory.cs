using System;

namespace FastCgiNet
{
	internal class NvpFactory
	{
		private static int GetLengthFromByteArray(byte[] arr, int offset, int length)
		{
			if ((arr[offset] >> 7) == 0)
			{
				// 1 byte long
				if (length < 1)
					throw new InsufficientBytesException();

				return arr[offset];
			}
			else
			{
				// 4 bytes long
				if (length < 4)
					throw new InsufficientBytesException();

				return ((arr[offset] & 0x7f) << 24) + (arr[offset + 1] << 16) + (arr[offset + 2] << 8) + arr[offset + 3];
			}
		}

		public static bool TryCreateNVP(byte[] firstData, int offset, int length, out NameValuePair createdNvp, out int endOfNvp)
		{
			if (ByteUtils.CheckArrayBounds(firstData, offset, length) == false)
				throw new InvalidOperationException(""); //TODO: Descriptive message

			// Gets the lengths of the name and the value
			int nameLength, valueLength, bytesRead;
			try
			{
				nameLength = GetLengthFromByteArray(firstData, offset, length);
				bytesRead = 0;
				
				if (nameLength > 0x7f)
				{
					bytesRead += 4;
					valueLength = GetLengthFromByteArray(firstData, offset + 4, length - 4);
				}
				else
				{
					bytesRead += 1;
					valueLength = GetLengthFromByteArray(firstData, offset + 1, length - 1);
				}
				bytesRead += (valueLength > 0x7f) ? 4 : 1;
			}
			catch (InsufficientBytesException)
			{
				endOfNvp = -1;
				createdNvp = null;
				return false;
			}

			createdNvp = new NameValuePair(nameLength, valueLength);
			
			// In case we got more than just the lengths, start defining the name and value
			if (length > bytesRead)
			{
				createdNvp.FeedBytes(firstData, offset + bytesRead, length - bytesRead, out endOfNvp);
			}
			else
			{
				endOfNvp = -1;
			}

			return true;
		}
	}
}

