using System;
using System.Collections.Generic;

namespace FastCgiNet
{
	public class NameValuePairCollection
	{
		int addedContentLength = 0;
		int addedPaddingLength = 0;
		public int ContentLength { get; private set; }
		public int PaddingLength { get; private set; }
		LinkedList<NameValuePair> content;
		public ICollection<NameValuePair> Content
		{
			get
			{
				return content;
			}
		}
		
		NameValuePair lastValuePair = null;
		internal void FeedBytes(byte[] data, int offset, int length, out int lastByteOfRecord)
		{
			// The length must be greater than zero because otherwise we don't know what to set lastByteOfRecord to..
			if (length <= 0)
				throw new ArgumentOutOfRangeException("length must be greater than zero");

			// Are we already done here? Check.
			if (ContentLength + PaddingLength == addedContentLength + PaddingLength)
			{
				throw new InvalidOperationException("This record is all set! You can't add more content or padding");
			}

			// Now feed every byte..
			int bytesFed = 0;
			int lastValuePairByteOffset;
			while (bytesFed < length && addedContentLength + bytesFed < ContentLength)
			{
				if (lastValuePair == null)
				{
					lastValuePair = new NameValuePair(data, offset + bytesFed, length - bytesFed, out lastValuePairByteOffset);
					content.AddLast(lastValuePair);
				}
				else
					lastValuePair.FeedBytes(data, offset + bytesFed, length - bytesFed, out lastValuePairByteOffset);

				//Console.WriteLine("Added NVP with last byte idx {0}", lastValuePairByteOffset);

				if (lastValuePairByteOffset == -1)
					break;

				bytesFed = lastValuePairByteOffset - offset + 1;
				//Console.WriteLine("Bytes fed so far: {0}", bytesFed);
				lastValuePair = null;
			}

			addedContentLength += bytesFed;

			// There could still be bytes available to feed for the padding, if the content was all added
			if (addedContentLength == ContentLength)
			{
				int paddingAvailable = length - bytesFed;
				int paddingNeeded = PaddingLength - addedPaddingLength;

				if (paddingAvailable >= paddingNeeded)
				{
					lastByteOfRecord = offset + bytesFed + paddingNeeded - 1;
					addedPaddingLength = PaddingLength;
				}
				else
				{
					lastByteOfRecord = -1;
					addedPaddingLength += paddingAvailable;
				}
			}
			else
			{
				//Console.WriteLine("Added COntent Length: {0}", addedContentLength);
				lastByteOfRecord = -1;
			}
		}
		
		internal NameValuePairCollection (int contentLength, int paddingLength)
		{
			ContentLength = contentLength;
			PaddingLength = paddingLength;
			content = new LinkedList<NameValuePair>();
		}
	}
}
