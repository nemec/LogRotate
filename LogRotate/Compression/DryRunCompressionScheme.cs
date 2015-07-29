using System;
using System.IO;
using System.Text;

namespace LogRotate.Compression
{
    public class DryRunCompressionScheme : ICompressionScheme
    {
        private readonly CompressionScheme _compression;

        public DryRunCompressionScheme(CompressionScheme compression)
        {
            _compression = compression;
        }

        public string Extension
        {
            get { return _compression.Extension; }
        }

        public Stream GetCompressionStream(FileStream stream)
        {
            throw new NotImplementedException();
        }

        public void AppendExtension(StringBuilder filenameBuilder)
        {
            _compression.AppendExtension(filenameBuilder);
        }
    }
}
