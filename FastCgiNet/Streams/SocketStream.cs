using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

namespace FastCgiNet.Streams
{
    public class SocketStream : FastCgiStream
    {
        private SocketRequest Request;
        public readonly RecordType RecordType;

        private RecordContentsStream LastFlushedStream;

        public override void Flush()
        {
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
                    using (var record = (StreamRecordBase)RecordFactory.CreateRecord(Request.RequestId, RecordType))
                    {
                        record.Contents = it.Current;
                        Request.Send(record);
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
            // Flush and send an empty record!
            this.Flush();
            using (var emptyRecord = (StreamRecordBase)RecordFactory.CreateRecord(Request.RequestId, RecordType))
            {
                Request.Send(emptyRecord);
            }

            base.Close();
        }

        public SocketStream(SocketRequest req, RecordType streamType)
        {
            if (req == null)
                throw new ArgumentNullException("req");
            else if (!streamType.IsStreamType())
                throw new ArgumentException("streamType must be a stream record type");

            Request = req;
            RecordType = streamType;
            LastFlushedStream = null;
        }
    }
}
