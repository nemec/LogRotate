using System.IO;
using System.IO.Compression;

namespace LogRotate.Compression
{
    internal class GZipCompression : CompressionScheme
    {
        public GZipCompression(int value, string displayName)
            : base(value, displayName)
        {
        }

        public override string Extension
        {
            get { return "gz"; }
        }

        public override Stream GetCompressionStream(FileStream stream)
        {
            return new GZipStream(stream, CompressionMode.Compress);
        }
    }
}