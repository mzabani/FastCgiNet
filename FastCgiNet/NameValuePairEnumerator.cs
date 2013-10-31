using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	internal class NameValuePairEnumerator : IEnumerator<NameValuePair>
	{
		private Stream Contents;
		private int bufsize = 128;
		private byte[] buf;
		private int unusedBytes;
		private NameValuePair lastIncompleteNvp;
		private int ContentLength;

		private void ShiftBufferLeftBy(int times)
		{
			for (int i = 0; i < bufsize - times; ++i)
				buf[i] = buf[i + times];
		}

		public bool MoveNext()
		{
			//TODO: NVPs split across different records.
			// Perhaps enumerating parameters should not be available in a ParamsRecord, nor adding them.
			// It makes sense for Stream records that a higher-level interface provides writing and reading from, one
			// that has no size limits and hides away record boundaries from the user.
			int readBytes;
			while ((readBytes = Contents.Read(buf, unusedBytes, bufsize - unusedBytes)) > 0)
			{
				// Do a gte instead of an equals check to be benevolent in case of malformed name value pairs
				if (Contents.Position >= ContentLength)
					break;

				int endOfNvp;
				if (lastIncompleteNvp == null)
				{
					if (NVPFactory.TryCreateNVP(buf, 0, unusedBytes + readBytes, out lastIncompleteNvp, out endOfNvp))
					{
						// We had enough bytes to create the nvp. Did we have enough for all of its contents too?
						if (endOfNvp >= 0)
						{
							currentNvp = lastIncompleteNvp;
							lastIncompleteNvp = null;
							ShiftBufferLeftBy(endOfNvp + 1);
							unusedBytes = unusedBytes + readBytes - endOfNvp - 1;
							return true;
						}
						else
						{
							unusedBytes = 0;
						}
					}
					else
					{
						// There weren't enough bytes to even create a nvp. Keep those bytes in the buffer as unused
						unusedBytes += readBytes;
					}
				}
				else
				{
					lastIncompleteNvp.FeedBytes(buf, 0, unusedBytes + readBytes, out endOfNvp);
					if (endOfNvp >= 0)
					{
						currentNvp = lastIncompleteNvp;
						lastIncompleteNvp = null;
						ShiftBufferLeftBy(endOfNvp + 1);
						unusedBytes = unusedBytes + readBytes - endOfNvp - 1;
						return true;
					}
					else
					{
						unusedBytes = 0;
					}
				}
			}

			// Reached the end of the stream, no more nvps from now on..
			return false;
		}
		public void Reset ()
		{
			throw new NotSupportedException();
		}
		object System.Collections.IEnumerator.Current
		{
			get
			{
				return currentNvp;
			}
		}

		public void Dispose()
		{
		}

		NameValuePair currentNvp;
		NameValuePair IEnumerator<NameValuePair>.Current
		{
			get
			{
				return currentNvp;
			}
		}

		/*internal void FeedBytes(byte[] data, int offset, int length, out int lastByteOfRecord)
		{
			// The length must be greater than zero because otherwise we don't know what to set lastByteOfRecord to..
			if (length <= 0)
				throw new ArgumentOutOfRangeException("length must be greater than zero");

			// Are we already done here? Check.
			if (expectedContentLength + expectedPaddingLength == addedexpectedContentLength + expectedPaddingLength)
			{
				throw new InvalidOperationException("This record is all set! You can't add more content or padding");
			}

			// Now feed every byte..
			int bytesFed = 0;
			int lastValuePairByteOffset;
			while (bytesFed < length && addedexpectedContentLength + bytesFed < expectedContentLength)
			{
				if (currentValuePair == null)
				{
					currentValuePair = new NameValuePair(data, offset + bytesFed, length - bytesFed, out lastValuePairByteOffset);
					Add(currentValuePair);
				}
				else
					currentValuePair.FeedBytes(data, offset + bytesFed, length - bytesFed, out lastValuePairByteOffset);

				if (lastValuePairByteOffset == -1)
					break;

				bytesFed = lastValuePairByteOffset - offset + 1;
				currentValuePair = null;
			}

			addedexpectedContentLength += bytesFed;

			// There could still be bytes available to feed for the padding, if the content was all added
			if (addedexpectedContentLength == expectedContentLength)
			{
				int paddingAvailable = length - bytesFed;
				int paddingNeeded = expectedPaddingLength - addedexpectedPaddingLength;

				if (paddingAvailable >= paddingNeeded)
				{
					lastByteOfRecord = offset + bytesFed + paddingNeeded - 1;
					addedexpectedPaddingLength = expectedPaddingLength;
				}
				else
				{
					lastByteOfRecord = -1;
					addedexpectedPaddingLength += paddingAvailable;
				}
			}
			else
			{
				lastByteOfRecord = -1;
			}
		}*/

		public NameValuePairEnumerator(Stream s, int contentLength)
		{
			Contents = s;
			ContentLength = contentLength;
			buf = new byte[bufsize];
		}
	}
}
