using System;
using System.IO;
using System.Collections.Generic;

namespace FastCgiNet.Streams
{
    public class NvpReader : IDisposable
    {
        private Stream ParamsStream;
        private IEnumerator<NameValuePair> Enumerator;

        /// <summary>
        /// Reads the next <seealso cref="FastCgiNet.NameValuePair"/>. This method advances the Params Stream.
        /// </summary>
        public NameValuePair Read()
        {
            if (Enumerator.MoveNext())
                return Enumerator.Current;
            else
                return null;
        }

        public void Dispose()
        {
            Enumerator.Dispose();
            ParamsStream.Dispose();
        }

        /// <summary>
        /// Provides an easy way to read NameValuePairs from a Params Stream. When disposed, disposes the stream passed in this constructor as well.
        /// </summary>
        public NvpReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            ParamsStream = stream;
            Enumerator = new NvpEnumerator(stream, stream.Length);
        }
    }
}
