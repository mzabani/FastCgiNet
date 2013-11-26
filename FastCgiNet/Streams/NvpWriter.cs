using System;

namespace FastCgiNet.Streams
{
    public class NvpWriter
    {
        private FastCgiStream ParamsStream;

        /// <summary>
        /// Writes a NameValuePair with name <paramref name="name"/> and value <paramref name="value"/> to the Stream.
        /// Make sure both strings can be ASCII encoded.
        /// </summary>
        public void Write(string name, string value)
        {
            Write(new NameValuePair(name, value));
        }

        /// <summary>
        /// Writes a NameValuePair to the Stream.
        /// </summary>
        public void Write(NameValuePair nvp)
        {
            if (nvp == null)
                throw new ArgumentNullException("nvp");

            foreach (var seg in nvp.GetBytes())
                ParamsStream.Write(seg.Array, seg.Offset, seg.Count);
        }

        public NvpWriter(FastCgiStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            ParamsStream = stream;
        }
    }
}
