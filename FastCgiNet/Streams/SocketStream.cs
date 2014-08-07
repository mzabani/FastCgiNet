using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

namespace FastCgiNet.Streams
{
    /// <summary>
    /// Represents a FastCgi stream (e.g. Stdin, Stdout or Stderr) over a socket.
    /// </summary>
    public class SocketStream : FastCgiStream, IDisposable
    {
        private Socket Socket;

        /// <summary>
        /// Before flushing or closing this stream, make sure you set a proper RequestId through this field.
        /// </summary>
        public ushort RequestId { get; set; }

        public readonly RecordType RecordType;

        private RecordContentsStream LastFlushedStream;

        private void Send(RecordBase rec)
        {
            foreach (var seg in rec.GetBytes())
                Socket.Send(seg.Array, seg.Offset, seg.Count, SocketFlags.None);
        }

        /// <summary>
        /// Sends all written data that has not been sent over the socket. If nothing has been written to the stream
        /// or everything has already been flushed, this method does not throw and does not send anything through the socket.
        /// If this stream is in read mode or has been closed or disposed, this method throws.
        /// </summary>
        public override void Flush()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("SocketStream");
            if (IsReadMode)
                throw new InvalidOperationException("Can't flush a SocketStream that is in Read Mode");

            IEnumerator<RecordContentsStream> it = UnderlyingStreams.GetEnumerator();
            try
            {
                // Move until the last flushed stream
                if (LastFlushedStream != null)
                {
                    while (it.MoveNext())
                    {
                        if (it.Current == LastFlushedStream)
                            break;
                    }
                }

                // Now flush the ones that haven't been flushed for real!
                while (it.MoveNext())
                {
                    // We have to make sure that the current stream is not empty,
                    // because that would mean us sending an empty record here, when it should only happen in Close()
                    if (it.Current.Length == 0)
                        continue;

                    using (var record = (StreamRecordBase)RecordFactory.CreateRecord(RequestId, RecordType))
                    {
                        record.Contents = it.Current;
                        Send(record);
                    }
                }
            }
            catch
            {
                it.Dispose();
                throw;
            }

            // Internal bookkeeping
            LastFlushedStream = LastUnfilledStream;
            var newLastStream = new RecordContentsStream();
            underlyingStreams.AddLast(newLastStream);
            LastUnfilledStream = newLastStream;
        }

        /// <summary>
        /// Flushes this stream (<see cref="Flush()"/>) and disposes it. If no data has been flushed, this method does nothing else. If some data was flushed,
        /// then this method flushes the remaining data and also sends an empty record through the socket, meaning end of stream to the other communicating party.
        /// If the stream is in read mode, this method does nothing.
        /// If this stream has already been disposed, this method does nothing, but does not throw either.
        /// This method never closes the underlying socket.
        /// </summary>
        public override void Close()
        {
            if (IsDisposed)
                return;

            // If this stream is empty, don't bother wasting network resources with an empty record
            // Or if this is in Read Mode, then we just don't send anything at all!
            if (Length == 0 || IsReadMode)
            {
                IsDisposed = true;
                return;
            }

            // Flush and send an empty record!
            this.Flush();
            using (var emptyRecord = (StreamRecordBase)RecordFactory.CreateRecord(RequestId, RecordType))
            {
                Send(emptyRecord);
            }
            IsDisposed = true;
        }

        private bool IsDisposed;
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            this.Close();
        }

        public SocketStream(Socket sock, RecordType streamType, bool readMode)
            : base(readMode)
        {
            if (sock == null)
                throw new ArgumentNullException("sock");
            else if (!streamType.IsStreamType())
                throw new ArgumentException("streamType must be a stream record type");

            Socket = sock;
            RecordType = streamType;
            IsDisposed = false;
            LastFlushedStream = null;
        }
    }
}
