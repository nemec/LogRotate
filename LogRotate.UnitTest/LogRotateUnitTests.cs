using System;
using System.Linq;
using LogRotate.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PathLib;

namespace LogRotate.UnitTest
{
    [TestClass]
    public class LogRotateUnitTests
    {
        private static readonly IPath TestDataDirectory = Path.Create("TestData");

        private IPath TempDirectory { get; set; }

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            if (!TestDataDirectory.Exists())
            {
                throw new ArgumentException("Test data directory cannot be found.");
            }
        }

        [TestInitialize]
        public void SetUpTemp()
        {
            while (TempDirectory == null || TempDirectory.Exists())
            {
                TempDirectory = Path.Create(
                    System.IO.Path.GetTempPath(),
                    Guid.NewGuid().ToString());
            }
            TempDirectory.Mkdir(true);
        }

        private IPath CopyTestFile(string filename)
        {
            var file = TestDataDirectory.Join(filename);
            if (!file.Exists())
            {
                throw new ArgumentException(String.Format(
                    "Test file {0} does not exist.", file), "filename");
            }
            var newFile = file.FileInfo.CopyTo(file.WithDirname(TempDirectory).ToString());
            return Path.Create(newFile.FullName);
        }

        [TestCleanup]
        public void CleanUpTemp()
        {
            TempDirectory.Delete(true);
        }

        [TestMethod]
        public void Rotate_WithLogIsAlwaysRotatedAndNumericalRotation_RotatesLog()
        {
            // Arrange
            const string logfileName = "log.txt";
            var logfile = CopyTestFile(logfileName);
            var strategy = new NumericalRotationStrategy();
            var options = new ConfigFileOptions
            {
                Cleanup = CleanupBehavior.Delete,
                Compress = CompressionScheme.None,
                MaxRotations = 10,
                Rotate = RotationSchedule.SizeOnly,
                WhenEmpty = LogFileEmptyBehavior.Error,
                WhenMissing = LogFileMissingBehavior.Error,
                Size = "1b"
            };
            var logRotator = new LogRotator(false);
            
            // Act
            logRotator.Rotate(logfile, strategy, options);

            // Assert
            Assert.IsTrue(logfile.WithFilename("log.1.txt").Exists());
        }

        //
    }
}
