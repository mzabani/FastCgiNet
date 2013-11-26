using System;
using FastCgiNet;

namespace FastCgiNet.Tests
{
    class FastCgiStreamImpl : FastCgiNet.Streams.FastCgiStream
    {
        public override void Flush()
        {
        }

        public FastCgiStreamImpl(bool readMode)
            : base(readMode)
        {
        }
    }
}
