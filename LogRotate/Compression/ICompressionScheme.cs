using System.ComponentModel;
using System.IO;

namespace LogRotate.Compression
{
    [TypeConverter(typeof(EnumerationTypeConverter<CompressionScheme>))]
    public interface ICompressionScheme
    {
        string Extension { get; }

        Stream GetCompressionStream(FileStream stream);
    }
}