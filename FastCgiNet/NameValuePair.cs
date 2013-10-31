using System;
using System.Text;
using System.Collections.Generic;

namespace FastCgiNet
{
	public class NameValuePair
	{
		public int NameLength { get; private set; }
		public int ValueLength { get; private set; }

		/// <summary>
		/// The number of bytes needed to represent this nvp's name.
		/// </summary>
		internal int BytesForName
		{
			get
			{
				if (NameLength < 128)
					return 1;
				else
					return 4;
			}
		}

		/// <summary>
		/// The number of bytes needed to represent this nvp's value.
		/// </summary>
		internal int BytesForValue
		{
			get
			{
				if (ValueLength < 128)
					return 1;
				else
					return 4;
			}
		}

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

		internal IEnumerable<byte[]> GetBytes()
		{
			yield return name;
			yield return value;
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

		internal NameValuePair(int nameLength, int valueLength)
		{
			NameLength = nameLength;
			ValueLength = valueLength;
			name = new byte[nameLength];
			value = new byte[valueLength];
		}
	}
}
