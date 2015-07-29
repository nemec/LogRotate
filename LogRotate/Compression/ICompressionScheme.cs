using System.ComponentModel;
using System.IO;
using System.Text;

namespace LogRotate.Compression
{
    [TypeConverter(typeof(EnumerationTypeConverter<CompressionScheme>))]
    public interface ICompressionScheme
    {
        void AppendExtension(StringBuilder filenameBuilder);

        string Extension { get; }

        Stream GetCompressionStream(FileStream stream);
    }
}