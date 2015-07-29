using LogRotate.Compression;
using PathLib;

namespace LogRotate
{
    public class ConfigFileOptions
    {
        public ConfigFileOptions()
        {
            Destination = null;
            Size = "1MB";
            Rotate = RotationSchedule.Daily;
            MaxRotations = 3;
            Cleanup = CleanupBehavior.Truncate;
        }

        public WindowsPath Destination { get; set; }

        public ICompressionScheme Compress { get; set; }

        public RotationSchedule Rotate { get; set; }

        public string Size { get; set; }

        public int MaxRotations { get; set; }

        public LogFileEmptyBehavior WhenEmpty { get; set; }

        public LogFileMissingBehavior WhenMissing { get; set; }

        public CleanupBehavior Cleanup { get; set; }
    }
}