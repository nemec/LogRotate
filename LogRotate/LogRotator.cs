using System.Linq;
using log4net;
using LogRotate.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using PathLib;

namespace LogRotate
{
    public class LogRotator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogRotator));

        private readonly bool _dryRun;
        public LogRotator(bool dryRun)
        {
            _dryRun = dryRun;
        }

        public bool Rotate(IPath sourceFile, IRotationStrategy rotationStrategy, ConfigFileOptions options, bool forceRotate = false)
        {
            return sourceFile.Parent().ListDir(sourceFile.Filename)
                .All(file => RotateSingle(file, rotationStrategy, options, forceRotate));
        }

        private bool RotateSingle(IPath sourceFile, IRotationStrategy rotationStrategy, ConfigFileOptions options, bool forceRotate = false)
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

            ICompressionScheme compression = options.Compress;
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
                    case LogfileEmptyBehavior.Skip:
                        Logger.InfoFormat("{0} is empty, skipping.", sourceFile.Filename);
                        return false;
                    case LogfileEmptyBehavior.Error:
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

            ShiftOldFiles(sourceFile, rotationStrategy, options.MaxRotations, _dryRun);
            
            CopyContents(sourceFile, destinationFile, compression, _dryRun);
            
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
                Logger.DebugFormat("Deleting old logs: {0}",
                    String.Join(", ", filesToDelete.Select(f => f.Filename)));
            }
            else
            {
                Logger.Debug("No logs to delete.");
            }

            while (!dryRun && filesToDelete.Count > 0)
            {
                var lastDeleted = filesToDelete.Pop();
                File.Delete(lastDeleted.ToString());
            }

            if (filesToRotate.Count == 0)
            {
                Logger.Debug("No logs to rotate.");
                return;
            }
            
            while (filesToRotate.Count > 0)
            {
                var sourceFile = filesToRotate.Pop();
                var destFile = rotationStrategy.GetNextRotationForFile(sourceFile);
                Logger.DebugFormat("Rotating file {0} to {1}",
                    sourceFile.Filename, destFile);

                if (!dryRun && !sourceFile.Equals(destFile))
                {
                    File.Move(sourceFile.ToString(), destFile.ToString());
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
