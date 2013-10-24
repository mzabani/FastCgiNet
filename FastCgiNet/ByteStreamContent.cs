using System;
using System.IO;

namespace FastCgiNet
{
	/// <summary>
	/// A class to deal with the contents of a Byte Stream Record.
	/// </summary>
	class ByteStreamContent : IDisposable
	{
		int ContentLength;
		int PaddingLength;
		int addedContentLength;
		int addedPaddingLength;

		/// <summary>
		/// When feeding content, this property will be the stream to hold the content fed. If this class is instantiated
		/// with a Stream that is not a <see cref="RecordContentsStream"/> passed to it, this property is null.
		/// </summary>
		internal RecordContentsStream ContentFed { get; private set; }

		/// <summary>
		/// When this class is instantiated with a Stream passed to it, this will point to that stream.
		/// </summary>
		public Stream Content { get; private set; }

		/// <summary>
		/// Passes a stream which will contain the content of the record. This Stream must correctly implement Read and Length.
		/// </summary>
		public ByteStreamContent(Stream s)
		{
			Content = s;
		}

		/// <summary>
		/// The good constructor when using a different source stream to hold the record's contents.
		/// </summary>
		public ByteStreamContent(RecordContentsStream s)
		{
			ContentFed = s;
		}

		public ByteStreamContent()
		{
			ContentFed = new RecordContentsStream();
		}

		internal ByteStreamContent (int contentLength, int paddingLength)
		{
			ContentLength = contentLength;
			PaddingLength = paddingLength;
			ContentFed = new RecordContentsStream();
		}

		/// <summary>
		/// Disposes of any underlying streams.
		/// </summary>
		public void Dispose() {
			if (ContentFed != null)
				ContentFed.Dispose();
			if (Content != null)
				Content.Dispose();
		}

		/// <summary>
		/// Adds more content data that has been read from the socket. If we have reached the end of this record's content, then <paramref name="lastByteOfContent"/> will be >= 0. Otherwise, it will be -1,
		/// meaning more data needs to be added to this Content.
		/// </summary>
		/// <remarks>Do not use this method to build a record's contents to be sent over the wire. Use the stream directly in that case.</remarks>
		internal void FeedBytes (byte[] data, int offset, int length, out int lastByteOfRecord)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException("The length to be copied has to be greater than zero"); 
			else if (ByteCopyUtils.CheckArrayBounds(data, offset, length) == false)
				throw new InvalidOperationException("Can't do this operation. It would go out of the array's bounds");
			else if (ContentFed == null)
				throw new InvalidOperationException("A custom stream is being used to write the contents of this record. Write to that stream instead of using this method");

			// Fill up the Content if we can
			int contentNeeded = ContentLength - addedContentLength;
			if (contentNeeded < length)
			{
				if (contentNeeded != 0)
				{
					ContentFed.Write(data, offset, contentNeeded);
					addedContentLength = ContentLength;
				}

				// Check for the end of padding too!
				int paddingAvailable = length - contentNeeded;
				int paddingNeeded = PaddingLength - addedPaddingLength;
				if (paddingAvailable >= paddingNeeded)
				{
					lastByteOfRecord = offset + contentNeeded + paddingNeeded - 1;
					addedPaddingLength = PaddingLength;
				}
				else
				{
					lastByteOfRecord = offset + contentNeeded + paddingAvailable - 1;
					addedPaddingLength += paddingAvailable;
				}
			}
			else
			{
				ContentFed.Write(data, offset, length);
				addedContentLength += length;
				lastByteOfRecord = -1;
			}
		}
	}
}
