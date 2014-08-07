using System;
using System.IO;

namespace FastCgiNet.Streams
{
    /// <summary>
    /// A seekable stream that creates a temporary file when first written to, then writing and reading to/from this created file.
    /// This <see cref="System.IO.Stream"/> implementation can be used to store stream records' contents from a FastCgi Request
    /// in secondary storage. When disposed, the created file is deleted.
    /// </summary>
    public class SecondaryStorageRequestStream : Stream
    {
        private FileStream fileStream;

        /// <summary>
        /// The path of the temporary file where the contents are stored. This will be <c>null</c> until
        /// this stream is written to.
        /// </summary>
        public string TemporaryFilePath
        {
            get
            {
                return fileStream == null ? null : fileStream.Name;
            }
        }

        private void BuildFileStream()
        {
            if (fileStream == null)
                fileStream = new FileStream(Path.GetTempFileName(), FileMode.Create);
        }

        protected override void Dispose(bool disposing)
        {
            if (fileStream != null)
            {
                string fileName = fileStream.Name;
                fileStream.Dispose();
                File.Delete(fileName);
            }
        }

        public override void Flush()
        {
            if (fileStream != null)
                fileStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return fileStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return fileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            fileStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BuildFileStream();
            fileStream.Write(buffer, offset, count);
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
                return fileStream == null ? 0 : fileStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return fileStream == null ? 0 : fileStream.Position;
            }
            set
            {
                fileStream.Position = value;
            }
        }

        public SecondaryStorageRequestStream()
        {
        }
    }
}
