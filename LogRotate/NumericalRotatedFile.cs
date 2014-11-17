using System.Text;
using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    public class NumericalRotatedFile : IRotatedFile
    {
        private NumericalRotatedFile()
        {
        }

        public NumericalRotatedFile(IPath sourceFile)
        {
            File = sourceFile;
        }

        public IPath File { get; private set; }

        public string Filename { get { return File.Filename; } }

        public long Size { get { return File.Stat().Size; } }

        public int? RotationCount { get; private set; }

        public IPath GetRelativeRotation(int? numIterations, ICompressionScheme compression)
        {
            var stringBuilder = new StringBuilder(File.Basename);
            if (RotationCount.HasValue || numIterations.HasValue)
            {
                var valueOrDefault = RotationCount.GetValueOrDefault();
                if (numIterations.HasValue)
                {
                    valueOrDefault += numIterations.Value;
                }
                stringBuilder.Append(".");
                stringBuilder.Append(valueOrDefault);
            }
            stringBuilder.Append(File.Extension);
            if (compression != null && !string.IsNullOrEmpty(compression.Extension))
            {
                stringBuilder.Append(".");
                stringBuilder.Append(compression.Extension);
            }
            return File.WithFilename(stringBuilder.ToString());
        }

        public IRotatedFile InNewDirectory(IPath path)
        {
            return new NumericalRotatedFile
            {
                File = path.WithFilename(File.Filename),
                RotationCount = RotationCount
            };
        }

        public override string ToString()
        {
            return File.ToString();
        }
    }
}