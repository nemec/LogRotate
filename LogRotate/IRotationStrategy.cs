using System.Collections.Generic;
using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    public interface IRotationStrategy
    {
        ICompressionScheme Compression { get; set; }

        IEnumerable<IPath> GetExistingRotations(IPath logFile);

        IPath GetLastRotatedFile(IPath logFile);

        IPath GetNextRotationForFile(IPath logFile);
    }
}
