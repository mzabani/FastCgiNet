using System;
using System.IO;

namespace FastCgiNet.Streams
{
    /// <summary>
    /// An implementation of <see cref="ISecondaryStorageOps"/> that creates files through <see cref="Path.GetTempFileName()"/>
    /// on the very first write. When disposed, the file, if created, is deleted.
    /// </summary>
    public class TmpFileSecondaryStorageOps : ISecondaryStorageOps
    {
        private FileStream fileStream;

        public void WriteToStorage(ArraySegment<byte> arrSegment)
        {
            if (fileStream == null)
                fileStream = new FileStream(Path.GetTempFileName(), FileMode.CreateNew);


        }

        public Stream ReadData()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (fileStream == null)
                return;

            string fileName = fileStream.Name;
            fileStream.Dispose();
            File.Delete(fileName);
        }

        /// <summary>
        /// An implementation of <see cref="ISecondaryStorageOps"/> that creates files through <see cref="Path.GetTempFileName()"/>
        /// on the very first write. No file is created or written to during the construction of this object.
        /// </summary>
        public TmpFileSecondaryStorageOps()
        {
        }
    }
}

