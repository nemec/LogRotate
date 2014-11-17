using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    public class DateRotationStrategy : IRotationStrategy
    {
        public const string DefaultDateFormat = "-yyyyMMdd";

        public CultureInfo DateTimeCulture { get; set; }

        public string DateFormat { get; set; }

        public DateRotationStrategy()
        {
            DateFormat = DefaultDateFormat;
            DateTimeCulture = CultureInfo.InvariantCulture;
        }

        public ICompressionScheme Compression { get; set; }

        /// <summary>
        /// Iterate through all 
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="minBound"></param>
        /// <returns></returns>
        private bool HasDateAppended(IPurePath logFile, int minBound = 0)
        {
            var basename = logFile.BasenameWithoutExtensions;

            // shortcut in case where format is fixed-length
            DateTime parsedDate;
            string possibleDate;
            if (basename.Length >= DateFormat.Length)
            {
                possibleDate = basename.Substring(
                    basename.Length - DateFormat.Length, DateFormat.Length);
                if (DateTime.TryParseExact(
                    possibleDate, DateFormat,
                    DateTimeCulture, DateTimeStyles.None,
                    out parsedDate))
                {
                    return true;
                }
            }

            for (var i = basename.Length - 1; i >= minBound; i--)
            {
                possibleDate = basename.Substring(i, basename.Length - i);
                if (DateTime.TryParseExact(
                    possibleDate, DateFormat,
                    DateTimeCulture, DateTimeStyles.None,
                    out parsedDate))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasDateAppended(IPurePath logFile, string baseName)
        {
            return HasDateAppended(logFile, baseName.Length);
        }

        public IEnumerable<IPath> GetExistingRotations(IPath logFile)
        {
            var basename = logFile.BasenameWithoutExtensions;
            return logFile.Parent().ListDir()
                .Where(file => HasDateAppended(file, basename));
        }

        public IPath GetLastRotatedFile(IPath logFile)
        {
            return GetExistingRotations(logFile).FirstOrDefault();
        }

        public IPath GetNextRotationForFile(IPath logFile)
        {
            if (HasDateAppended(logFile))
            {
                return logFile;
            }
            var ext = String.Join("", logFile.Extensions);
            if (Compression != null && ext.EndsWith(Compression.Extension))
            {
                ext += "." + Compression.Extension;
            }
            var date = DateTime.Today.ToString(DateFormat);
            return logFile.WithFilename(logFile.BasenameWithoutExtensions + date + ext);
        }
    }
}
