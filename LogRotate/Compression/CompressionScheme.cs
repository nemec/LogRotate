using System;
using System.ComponentModel;
using System.IO;
using System.Text;

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

        public void AppendExtension(StringBuilder filenameBuilder)
        {
            if (!String.IsNullOrEmpty(Extension) && 
                !filenameBuilder.ToString().EndsWith("." + Extension))
            {
                filenameBuilder.Append(".");
                filenameBuilder.Append(Extension);
            }
        }

        public abstract string Extension { get; }

        public abstract Stream GetCompressionStream(FileStream stream);
    }
}