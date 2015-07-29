using System.Linq;
using log4net;
using LogRotate.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using PathLib;

namespace LogRotate
{
    /// <summary>
    /// Archives old logs with a given number of options.
    /// </summary>
    public class LogRotator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogRotator));

        public LogRotator(bool dryRun)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="rotationStrategy"></param>
        /// <param name="options"></param>
        /// <param name="forceRotate"></param>
        /// <returns></returns>
        public bool Rotate(IPath sourceFile, IRotationStrategy rotationStrategy, ConfigFileOptions options, bool forceRotate = false, bool dryRun = false)
        {
            // Allows sourceFile to represent a glob.
            return sourceFile.Parent().ListDir(sourceFile.Filename)
                .All(file => RotateSingle(file, rotationStrategy, options, forceRotate, dryRun));
        }

        private bool RotateSingle(IPath sourceFile, IRotationStrategy rotationStrategy, ConfigFileOptions options, bool forceRotate, bool dryRun)
        {
            if (!sourceFile.Exists())
            {
                var message = string.Format("File {0} does not exist.", sourceFile);
                if (options.WhenMissing == LogFileMissingBehavior.Error)
                    throw new ArgumentException(message);
                Logger.Warn(message);
                return false;
            }

            var destinationDir = options.Destination;
            if (destinationDir != null)
            {
                if (!destinationDir.Exists())
                {
                    throw new ArgumentException(
                        String.Format("Destination directory '{0}' does not exist.", destinationDir));
                }
                if (!destinationDir.IsDir())
                {
                    throw new ArgumentException(
                        String.Format("Destination path '{0}' is not a directory.", destinationDir));
                }
            }

            var maxFileSizeBytes = ByteConverter.ParseString(options.Size);

            if (!forceRotate && 
                !IsOldEnoughToRotate(sourceFile, rotationStrategy, options.Rotate) && 
                !IsLargeEnoughToRotate(sourceFile, maxFileSizeBytes))
            {
                Logger.WarnFormat("{0} is not old or large enough to rotate, skipping.", sourceFile);
                return false;
            }

            if (sourceFile.Stat().Size == 0L)
            {
                switch (options.WhenEmpty)
                {
                    case LogFileEmptyBehavior.Skip:
                        Logger.InfoFormat("{0} is empty, skipping.", sourceFile.Filename);
                        return false;
                    case LogFileEmptyBehavior.Error:
                        throw new InvalidOperationException(string.Format("Source file {0} is empty. Cannot rotate.", sourceFile.Filename));
                    default:
                        Logger.InfoFormat("{0} is empty, rotating empty file.", sourceFile.Filename);
                        break;
                }
            }

            var destinationFile = destinationDir == null
                ? rotationStrategy.GetNextRotationForFile(sourceFile) 
                : rotationStrategy.GetNextRotationForFile(
                    sourceFile.WithDirname(destinationDir.ToString()));
            
            Logger.InfoFormat("Rotating {0} to {1}", sourceFile, destinationFile);

            ShiftOldFiles(sourceFile, rotationStrategy, options.MaxRotations, dryRun);

            CopyContents(sourceFile, destinationFile, options.Compress, dryRun);
            
            options.Cleanup.Cleanup(sourceFile);

            return true;
        }

        private static bool IsOldEnoughToRotate(IPath sourceFile, 
            IRotationStrategy rotationStrategy, 
            RotationSchedule schedule)
        {
            var dateTime = GetLastRotatedDate(sourceFile, rotationStrategy) 
                ?? sourceFile.Stat().CTime;
            return schedule.ShouldRotate(dateTime);
        }

        private static DateTime? GetLastRotatedDate(IPath file, IRotationStrategy rotationStrategy)
        {
            var relativeRotation = rotationStrategy.GetLastRotatedFile(file);
            if (relativeRotation != null && relativeRotation.Exists())
            {
                return relativeRotation.Stat().CTime;
            }
            return null;
        }

        private static bool IsLargeEnoughToRotate(IPath file, long maxFileSizeBytes)
        {
            if (maxFileSizeBytes > 0L)
            {
                return maxFileSizeBytes < file.Stat().Size;
            }
            return false;
        }

        private static void ShiftOldFiles(IPath file, 
            IRotationStrategy rotationStrategy, 
            int maxRotations,
            bool dryRun)
        {
            var files = rotationStrategy
                .GetExistingRotations(file)
                .ToList();

            var filesToRotate = new Stack<IPath>();
            var filesToDelete = new Stack<IPath>();

            if (maxRotations > 0)
            {
                foreach (var fileToRotate in files.Take(maxRotations - 1))
                {
                    filesToRotate.Push(fileToRotate);
                }
                foreach (var fileToDelete in files.Skip(maxRotations - 1))
                {
                    filesToDelete.Push(fileToDelete);
                }
            }
            else
            {
                foreach (var fileToRotate in files)
                {
                    filesToRotate.Push(fileToRotate);
                }
            }

            if (filesToRotate.Any())
            {
                Logger.DebugFormat("Rotating archived logs: {0}",
                    String.Join(", ", filesToRotate.Select(f => f.Filename)));
            }
            if (filesToDelete.Any())
            {
                Logger.DebugFormat("Deleting archived logs: {0}",
                    String.Join(", ", filesToDelete.Select(f => f.Filename)));
            }
            else
            {
                Logger.Debug("No archived logs to delete.");
            }

            while (!dryRun && filesToDelete.Count > 0)
            {
                var lastDeleted = filesToDelete.Pop();
                lastDeleted.FileInfo.Delete();
            }

            if (filesToRotate.Count == 0)
            {
                Logger.Debug("No archived logs to rotate.");
                return;
            }
            
            while (filesToRotate.Count > 0)
            {
                var sourceFile = filesToRotate.Pop();
                var destFile = rotationStrategy.GetNextRotationForFile(sourceFile);
                if (sourceFile.Equals(destFile))
                {
                    continue;
                }
                Logger.DebugFormat("Rotating file {0} to {1}",
                    sourceFile.Filename, destFile);

                if (!dryRun && !sourceFile.Equals(destFile))
                {
                    sourceFile.FileInfo.MoveTo(destFile.ToString());
                }
            }
        }

        private static void CopyContents(IPath source, IPath destination, ICompressionScheme compression, bool dryRun)
        {
            if (dryRun)
            {
                return;
            }
            using (var sourceStream = source.Open(FileMode.Open))
            {
                using (var destinationStream = destination.Open(FileMode.Create))
                {
                    using (var compressionStream = compression.GetCompressionStream(destinationStream))
                        sourceStream.CopyTo(compressionStream);
                }
            }
        }
    }
}
