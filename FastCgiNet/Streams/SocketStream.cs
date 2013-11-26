using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

namespace FastCgiNet.Streams
{
    public class SocketStream : FastCgiStream
    {
        private Socket Socket;

        /// <summary>
        /// Before flushing or closing this stream, make sure you set a proper RequestId through this field.
        /// </summary>
        public ushort RequestId;

        public readonly RecordType RecordType;

        private RecordContentsStream LastFlushedStream;

        private void Send(RecordBase rec)
        {
            foreach (var seg in rec.GetBytes())
                Socket.Send(seg.Array, seg.Offset, seg.Count, SocketFlags.None);
        }

        public override void Flush()
        {
            // If we are in read mode or if this has already been disposed, don't send anything at all!
            if (IsReadMode || IsDisposed)
                return;

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
                        break;

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
            LastFlushedStream = lastUnfilledStream;
            var newLastStream = new RecordContentsStream();
            underlyingStreams.AddLast(newLastStream);
            lastUnfilledStream = newLastStream;
        }

        public override void Close()
        {
            // If this stream is empty, don't bother wasting network resources with an empty record
            // Or if this is in Read Mode, then we just don't send anything at all!
            // Or if this has already been disposed, don't do anything either!
            if (Length == 0 || IsReadMode || IsDisposed)
                return;

            // Flush and send an empty record!
            this.Flush();
            using (var emptyRecord = (StreamRecordBase)RecordFactory.CreateRecord(RequestId, RecordType))
            {
                Send(emptyRecord);
            }

            base.Close();
        }

        private bool IsDisposed;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDisposed = true;
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
            LastFlushedStream = null;
        }
    }
}
