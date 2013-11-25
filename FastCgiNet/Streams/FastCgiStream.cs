using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet.Streams
{
	public abstract class FastCgiStream : Stream
	{
        private long position;

        /// <summary>
        /// When this stream is in Read Mode (see <see cref="ReadMode"/>), this property can be used to find out if the other party has finished writing this stream.
        /// </summary>
        public bool Complete { get; internal set; }

        /// <summary>
        /// True when we are receiving records sent by the other party, false when we are to send data to the other party. Implementing streams must carefully define semantics
        /// in light of this value.
        /// If this Stream is in Read Mode, writes should throw.
        /// If it is not in Read Mode, then <see cref="Flush()"/> should send unsent data and <see cref="Close()"/> should flush
        /// and send an empty record indicating the end of the stream to the other communicating party.
        /// </summary>
        public bool ReadMode { get; private set; }

        protected LinkedList<RecordContentsStream> underlyingStreams;
        public IEnumerable<RecordContentsStream> UnderlyingStreams
		{
			get
			{
				return underlyingStreams;
			}
		}

        public void AppendStream(RecordContentsStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            underlyingStreams.AddLast(stream);
        }

        protected RecordContentsStream lastUnfilledStream;
		
        /*/// <summary>
		/// The last unfilled stream that contains part of the stream's contents. If this stream has length zero, then this stream
		/// has never been written to.
		/// </summary>
        public RecordContentsStream LastUnfilledStream
		{
			get
			{
				return lastUnfilledStream;
			}
		}*/
        
		#region Implemented abstract members of Stream
        public override int Read(byte[] buffer, int offset, int count)
		{
            long bytesSkipped = 0;
            int totalBytesRead = 0;
            foreach (var stream in underlyingStreams)
            {
                // Only start reading in the first stream that contains data according to the stream's current position
                if (bytesSkipped + stream.Length - 1 < position)
                {
                    bytesSkipped += stream.Length;
                    continue;
                }
                
                // If Read gets called more than once, we may be reading from an advanced stream, so rewind it.
                // This is also necessary since an advanced stream may be added to this.
                // We must, however, seek it back to its original position after reading from it
                long streamInitialPos = stream.Position;
                stream.Position = 0;
                
                // If this is the first stream, skip bytes that we don't want.
                // We must read either up to "count" bytes or the entire current stream, whichever one is smaller.
                int bytesToRead = count - totalBytesRead;
                if (bytesToRead == 0)
                    break;
                
                if (totalBytesRead == 0)
                {
                    stream.Seek(position - bytesSkipped, SeekOrigin.Begin);
                    if (stream.Length - (position - bytesSkipped) < bytesToRead)
                        bytesToRead = (int)(stream.Length - (position - bytesSkipped));
                }
                else if (bytesToRead > stream.Length)
                    bytesToRead = (int)stream.Length;
                
                totalBytesRead += stream.Read(buffer, offset + totalBytesRead, bytesToRead);
                stream.Seek(streamInitialPos, SeekOrigin.Begin);
            }
            
            position += totalBytesRead;
            return totalBytesRead;
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
            if (origin == SeekOrigin.Begin)
                position = offset;
            else if (origin == SeekOrigin.Current)
                position += offset;
            else
                position = Length + offset;
            
            return position;
		}
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
            if (position != Length)
                throw new NotImplementedException("Still only able to write to the end of the stream");

			int bytesCopied = 0;
			while (bytesCopied != count)
			{
				int bytesToCopy = RecordBase.MaxContentLength - (int)lastUnfilledStream.Length;
				if (bytesToCopy > count - bytesCopied)
					bytesToCopy = count - bytesCopied;

				if (bytesToCopy > 0)
				{
					lastUnfilledStream.Write(buffer, offset + bytesCopied, bytesToCopy);
					bytesCopied += bytesToCopy;
				}

				if (lastUnfilledStream.Length == RecordBase.MaxContentLength && bytesCopied < count)
				{
					// New lastStream
					lastUnfilledStream = new RecordContentsStream();
					underlyingStreams.AddLast(lastUnfilledStream);
				}
			}
		}
		public override bool CanRead
        {
			get
            {
                return true;
			}
		}
		public override bool CanSeek
        {
			get
            {
                return true;
			}
		}
		public override bool CanWrite
        {
			get
            {
                return true;
			}
		}
		public override long Length
        {
			get
            {
				return underlyingStreams.Sum(s => s.Length);
			}
		}
		public override long Position
        {
			get
            {
                return position;
			}
			set
            {
				Seek(value, SeekOrigin.Begin);
			}
		}
		#endregion

        //TODO: Different constructors for when the other party is sending us records that will be appended to this stream
        // (in this case Writes are forbidden)
        // and for when we want to write for the other party to receive
        // (in this case Writes are allowed, Flushing means sending unsent data, and Closing means sending everything + an empty record)

		public FastCgiStream()
		{
			underlyingStreams = new LinkedList<RecordContentsStream>();
			lastUnfilledStream = new RecordContentsStream();
			underlyingStreams.AddLast(lastUnfilledStream);
            position = 0;
		}
	}
}
