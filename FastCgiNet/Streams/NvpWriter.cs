using System;
using System.Linq;
using System.IO;

namespace FastCgiNet.Streams
{
    public class NvpWriter : IDisposable
    {
        private Stream ParamsStream;

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
        
        private static string[] ValidMethods = new string[] { "GET", "POST", "PUT", "DELETE", "HEAD" }; //TODO: Other Http 1.1 methods
        /// <summary>
        /// Writes the FastCgi parameters that would be created for an HTTP 1.1 request with method <paramref name="method"/> at
        /// Url <paramref name="u"/>.
        /// </summary>
        /// <param name="u">A properly Url-Escaped url.</param>
        /// <param name="method">The HTTP 1.1 method. It must be written upper case.</param>
        public void WriteParamsFromUri(Uri u, string method)
        {
            if (u == null)
                throw new ArgumentNullException("url");
            else if (method == null)
                throw new ArgumentNullException("method");
            else if (ValidMethods.Contains(method) == false)
                throw new ArgumentException("Method is not valid. Make sure it is a valid upper case HTTP/1.1 method");
            
            Write("HTTP_HOST", u.Host);
            if (u.Scheme == Uri.UriSchemeHttps)
                Write("HTTPS", "on");
            Write("SCRIPT_NAME", u.AbsolutePath);
            Write("DOCUMENT_URI", u.AbsolutePath);
            Write("REQUEST_METHOD", method);
            Write("SERVER_NAME", u.Host);
            Write("QUERY_STRING", u.Query);
            Write("REQUEST_URI", u.AbsolutePath + "/" + u.Query);
            Write("SERVER_PROTOCOL", "HTTP/1.1");
            Write("GATEWAY_INTERFACE", "CGI/1.1");
        }

        public void Dispose()
        {
            ParamsStream.Dispose();
        }

        public NvpWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            ParamsStream = stream;
        }
    }
}
