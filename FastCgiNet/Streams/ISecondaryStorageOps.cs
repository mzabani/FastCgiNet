using System;
using System.Collections.Generic;
using System.IO;

namespace FastCgiNet.Streams
{
    /// <summary>
    /// Implement this interface to allow FastCgi requests to write the received data to secondary storage in case of large requests.
    /// An implementation of this interface will be used by only one <see cref="FastCgiRequest"/> at a time.
    /// </summary>
    public interface ISecondaryStorageOps : IDisposable
    {
        /// <summary>
        /// Writes a record's stream to secondary storage.
        /// </summary>
        void WriteToStorage(ArraySegment<byte> arrSegment);

        /// <summary>
        /// Gets a stream that enables reading all the data that was written.
        /// </summary>
        Stream ReadData();
    }
}
