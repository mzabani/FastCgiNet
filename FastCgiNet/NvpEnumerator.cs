using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet
{
	internal class NvpEnumerator : IEnumerator<NameValuePair>
	{
		private Stream Contents;
		private int bufsize;
		private byte[] buf;
		private int unusedBytes;
		private NameValuePair lastIncompleteNvp;
		private long ContentLength;
		private long NvpBytesYielded;

		private void ShiftBufferLeftBy(int times)
		{
			for (int i = 0; i < bufsize - times; ++i)
				buf[i] = buf[i + times];
		}

		public bool MoveNext()
		{
            //TODO: Improve this implementation. Specifically, don't shift the buffer left every time. Keep a smaller buffer
            // that is big enough to create a Nvp and use it like ByteReader does
			int readBytes;
			while (true)
			{
				// Know when to stop
				if (NvpBytesYielded == ContentLength)
					return false;

				// No need to read from the stream if our yielded bytes plus unusedBytes is equal to the expected content length
				if (unusedBytes + NvpBytesYielded < ContentLength)
				{
					int bytesToRead = bufsize - unusedBytes;
					if (bytesToRead > ContentLength - (unusedBytes + NvpBytesYielded))
						bytesToRead = (int)(ContentLength - (unusedBytes + NvpBytesYielded));

					readBytes = Contents.Read(buf, unusedBytes, bytesToRead);
				}
				else
					readBytes = 0;

				int endOfNvp;
				if (lastIncompleteNvp == null)
				{
					if (NvpFactory.TryCreateNvp(buf, 0, unusedBytes + readBytes, out lastIncompleteNvp, out endOfNvp))
					{
						// We had enough bytes to create the nvp. Did we have enough for all of its contents too?
						if (endOfNvp >= 0)
						{
							currentNvp = lastIncompleteNvp;
							lastIncompleteNvp = null;
							ShiftBufferLeftBy(endOfNvp + 1);
							unusedBytes = unusedBytes + readBytes - endOfNvp - 1;
							NvpBytesYielded += currentNvp.GetBytes().Sum(nvpB => nvpB.Count);
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
						NvpBytesYielded += currentNvp.GetBytes().Sum(nvpB => nvpB.Count);
						return true;
					}
					else
					{
						unusedBytes = 0;
					}
				}
			}
		}
		public void Reset()
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

		private NameValuePair currentNvp;
		NameValuePair IEnumerator<NameValuePair>.Current
		{
			get
			{
				return currentNvp;
			}
		}

		public NvpEnumerator(Stream s, long contentLength)
		{
			Contents = s;
			ContentLength = contentLength;
			NvpBytesYielded = 0;

			bufsize = 128;
			buf = new byte[bufsize];
		}
	}
}
