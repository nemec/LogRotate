using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    /// <summary>
    /// Add an integer suffix to the end of the basename, with lower
    /// integers being more recent logs.
    /// </summary>
    public class NumericalRotationStrategy : IRotationStrategy
    {
        public ICompressionScheme Compression { get; set; }

        public IEnumerable<IPath> GetExistingRotations(IPath logFile)
        {
            var pattern = new StringBuilder();
            pattern.Append(logFile.Basename);
            pattern.Append(@"[.]\d+");
            pattern.Append(logFile.Extension);
            if (Compression != null &&
                !String.IsNullOrEmpty(Compression.Extension) && 
                logFile.Extension != "." + Compression.Extension)
            {
                pattern.Append(".");
                pattern.Append(Compression.Extension);
            }

            return logFile
                .Parent()
                .ListDir()
                .Where(f => Regex.IsMatch(f.Filename, pattern.ToString()))
                .OrderBy(k => k.Filename);
        }

        public IPath GetLastRotatedFile(IPath logFile)
        {
            return GetExistingRotations(logFile).FirstOrDefault();
        }

        public IPath GetNextRotationForFile(IPath logFile)
        {
            var fnameBuilder = new StringBuilder();
            fnameBuilder.Append(logFile.BasenameWithoutExtensions);

            var extensions = logFile.Extensions;

            var currentRotation = 0;
            var extNumIdx = 1;  // Extension index from rightmost extension
            if (Compression != null && logFile.Extension == "." + Compression.Extension)
            {
                extNumIdx++;
            }
            if (extNumIdx < extensions.Length)  // existing rotation
            {
                Int32.TryParse(
                    extensions[extensions.Length - 1 - extNumIdx].TrimStart('.'), 
                    out currentRotation);
            }

            currentRotation++;

            // Add pre-extensions
            foreach (var extension in logFile.Extensions.Take(extensions.Length - 1 - extNumIdx))
            {
                fnameBuilder.Append(extension);
            }

            fnameBuilder.Append(".");
            fnameBuilder.Append(currentRotation);

            // Add post-extensions
            foreach (var extension in logFile.Extensions.Skip(extensions.Length - extNumIdx))
            {
                fnameBuilder.Append(extension);
            }

            if (Compression != null)
            {
                Compression.AppendExtension(fnameBuilder);
            }

            return logFile.WithFilename(fnameBuilder.ToString());
        }
    }
}
