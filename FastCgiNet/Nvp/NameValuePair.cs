using System;
using System.Text;
using System.Collections.Generic;

namespace FastCgiNet
{
	public class NameValuePair
	{
		public int NameLength { get; private set; }
		public int ValueLength { get; private set; }

		private int nameAndValueLengthSoFar = 0;
		private byte[] nameBytes;
		private byte[] valueBytes;
		public string Name
		{
			get
			{
                return System.Text.ASCIIEncoding.ASCII.GetString(nameBytes);
			}
		}
		public string Value
		{
			get
			{
                return System.Text.ASCIIEncoding.ASCII.GetString(valueBytes);
			}
		}

		private byte[] GetHeaderBytes(int length)
		{
			byte[] headerBytes;

			if (length < 128)
			{
				headerBytes = new byte[1] { (byte)length };
			}
			else
			{
				// MSB first

				headerBytes = new byte[4];
				headerBytes[3] = (byte) (length & 0xff000000);
				headerBytes[2] = (byte) (length & 0xff0000);
				headerBytes[1] = (byte) (length & 0xff00);
				headerBytes[0] = (byte) (length & 0xff);
			}

			return headerBytes;
		}

		internal IEnumerable<ArraySegment<byte>> GetBytes()
		{
			// We have to build the header bytes, i.e. the name and value lengths 
			yield return new ArraySegment<byte>(GetHeaderBytes(NameLength));
			yield return new ArraySegment<byte>(GetHeaderBytes(ValueLength));

			// Just return the name and value now, simple!
			yield return new ArraySegment<byte>(nameBytes);
			yield return new ArraySegment<byte>(valueBytes);
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
				Array.Copy(nameOrValueData, offset, valueBytes, nameAndValueLengthSoFar - NameLength, length);
				nameAndValueLengthSoFar += length;
			}
			else if (length <= NameLength - nameAndValueLengthSoFar)
			{
				Array.Copy(nameOrValueData, offset, nameBytes, nameAndValueLengthSoFar, length);
				nameAndValueLengthSoFar += length;
			}
			else
			{
				int lengthLeftForName = NameLength - nameAndValueLengthSoFar;
				Array.Copy(nameOrValueData, offset, nameBytes, nameAndValueLengthSoFar, lengthLeftForName);
				Array.Copy(nameOrValueData, offset + lengthLeftForName, valueBytes,  0, length - lengthLeftForName);
				nameAndValueLengthSoFar += length;
			}

			if (nameAndValueLengthSoFar == NameLength + ValueLength)
				lastByteOfNameValuePair = offset + length - 1;
		}

		/// <summary>
		/// Creates a name value pair with name <paramref name="name"/> and value <paramref name="value"/>.
		/// Make sure both strings can be ASCII encoded.
		/// </summary>
		public NameValuePair(string name, string val)
		{
            if (name == null)
                throw new ArgumentNullException("name");
            else if (val == null)
                throw new ArgumentNullException("val");

			nameBytes = ASCIIEncoding.ASCII.GetBytes(name);
			valueBytes = ASCIIEncoding.ASCII.GetBytes(val);
			NameLength = nameBytes.Length;
			ValueLength = valueBytes.Length;
			nameAndValueLengthSoFar = NameLength + ValueLength;
		}

		internal NameValuePair(int nameLength, int valueLength)
		{
			NameLength = nameLength;
			ValueLength = valueLength;
			nameBytes = new byte[nameLength];
			valueBytes = new byte[valueLength];
		}
	}
}
