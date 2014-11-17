using System.ComponentModel;
using System.IO;

namespace LogRotate.Compression
{
    [TypeConverter(typeof(EnumerationTypeConverter<CompressionScheme>))]
    public abstract class CompressionScheme : Enumeration<CompressionScheme>, ICompressionScheme
    {
        public static readonly CompressionScheme GZip = new GZipCompression(0, "gzip");
        public static readonly CompressionScheme None = new NoCompression(1, "none");

        protected CompressionScheme(int value, string displayName) 
            : base(value, displayName)
        {
        }

        public abstract string Extension { get; }

        public abstract Stream GetCompressionStream(FileStream stream);
    }
}