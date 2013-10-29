using System;
using System.Text;

namespace FastCgiNet
{
	public class NameValuePair
	{
		public int NameLength { get; private set; }
		public int ValueLength { get; private set; }

		private int nameAndValueLengthSoFar = 0;
		private byte[] name;
		private byte[] value;
		public string Name {
			get
			{
				var x = new StringBuilder(NameLength);
				foreach (byte b in name)
					x.Append((char)b);

				return x.ToString();
			}
		}
		public string Value {
			get
			{
				var x = new StringBuilder(ValueLength);
				foreach (byte b in value)
				{
					x.Append((char)b);
				}
				
				return x.ToString();
			}
		}

		internal void FeedBytes (byte[] nameOrValueData, int offset, int length, out int lastByteOfNameValuePair) {
			int bytesLeftTillEndOfThisNVP = NameLength + ValueLength - nameAndValueLengthSoFar;
			if (bytesLeftTillEndOfThisNVP < 0)
			{
				lastByteOfNameValuePair = NameLength + ValueLength - nameAndValueLengthSoFar + offset - 1;
				return;
			}
			else
			{
				// Signal that we didn't reach the end yet and fix the length parameter for everything else
				lastByteOfNameValuePair = -1;
				int maximumLength = NameLength + ValueLength - nameAndValueLengthSoFar;
				if (maximumLength < length)
					length = maximumLength;
			}

			// Now fill the Name and Value arrays
			if (nameAndValueLengthSoFar >= NameLength)
			{
				Array.Copy(nameOrValueData, offset, value, nameAndValueLengthSoFar - NameLength, length);
				nameAndValueLengthSoFar += length;
			}
			else if (length <= NameLength - nameAndValueLengthSoFar)
			{
				Array.Copy(nameOrValueData, offset, name, nameAndValueLengthSoFar, length);
				nameAndValueLengthSoFar += length;
			}
			else
			{
				int lengthLeftForName = NameLength - nameAndValueLengthSoFar;
				Array.Copy(nameOrValueData, offset, name, nameAndValueLengthSoFar, lengthLeftForName);
				Array.Copy(nameOrValueData, offset + lengthLeftForName, value,  0, length - lengthLeftForName);
				nameAndValueLengthSoFar += length;
			}

			if (nameAndValueLengthSoFar == NameLength + ValueLength)
				lastByteOfNameValuePair = offset + length - 1;
		}

		private int GetLengthFromByteArray (byte[] arr, int offset)
		{
			if ((arr[offset] >> 7) == 0)
			{
				// 1 byte long
				return arr[offset];
			}
			else
			{
				// 4 bytes long
				return ((arr[offset] & 0x7f) << 24) + (arr[offset + 1] << 16) + (arr[offset + 2] << 8) + arr[offset + 3];
			}
		}

		/// <summary>
		/// Creates a name value pair with name <paramref name="key"/> and value <paramref name="value"/>.
		/// Make sure both strings can be ASCII encoded.
		/// </summary>
		public NameValuePair(string key, string val)
		{
			name = ASCIIEncoding.ASCII.GetBytes(key);
			value = ASCIIEncoding.ASCII.GetBytes(val);
			NameLength = name.Length;
			ValueLength = value.Length;
			nameAndValueLengthSoFar = NameLength + ValueLength;
		}

		internal NameValuePair (byte[] firstData, int offset, int length, out int lastByteOfNameValuePair)
		{
			//TODO: Make length verifications. We need a minimum amount of bytes to work with here..

			// Gets the lenghts of the name and the value
			NameLength = GetLengthFromByteArray(firstData, offset);
			int bytesRead = 0;

			if (NameLength > 0x7f)
			{
				bytesRead += 4;
				ValueLength = GetLengthFromByteArray(firstData, offset + 4);
			}
			else
			{
				bytesRead += 1;
				ValueLength = GetLengthFromByteArray(firstData, offset + 1);
			}
			bytesRead += (ValueLength > 0x7f) ? 4 : 1;

			// Alloc with the right sizes
			name = new byte[NameLength];
			value = new byte[ValueLength];

			// In case we got more than just the lengths, start defining the name and value
			if (length > bytesRead)
			{
				FeedBytes(firstData, offset + bytesRead, length - bytesRead, out lastByteOfNameValuePair);
			}
			else
				lastByteOfNameValuePair = -1;
		}
	}
}
