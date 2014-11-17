using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    public interface IRotatedFile
    {
        IPath File { get; }
        string Filename { get; }
        long Size { get; }
        IPath GetRelativeRotation(int? numIterations, ICompressionScheme compression);
        IRotatedFile InNewDirectory(IPath path);
    }
}