using System.IO;

namespace LogRotate.Compression
{
    public class NoCompression : CompressionScheme
    {
        public NoCompression(int value, string displayName)
            : base(value, displayName)
        {
        }

        public override string Extension
        {
            get { return ""; }
        }

        public override Stream GetCompressionStream(FileStream stream)
        {
            return stream;
        }
    }
}