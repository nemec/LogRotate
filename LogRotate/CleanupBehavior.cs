using System.ComponentModel;
using System.IO;
using PathLib;

namespace LogRotate
{
    [TypeConverter(typeof(EnumerationTypeConverter<CleanupBehavior>))]
    public abstract class CleanupBehavior : Enumeration<CleanupBehavior>
    {
        public static CleanupBehavior
            None = new NoneBehavior(),
            Truncate = new TruncateBehavior(),
            Delete = new DeleteBehavior(),
            Recreate = new RecreateBehavior();

        private CleanupBehavior(string displayName) 
            : base(displayName)
        {
        }

        public abstract void Cleanup(IPath file);

        private class NoneBehavior : CleanupBehavior
        {
            public NoneBehavior()
                : base("none")
            {
            }

            public override void Cleanup(IPath file)
            {
                // Do nothing...
            }
        }

        private class TruncateBehavior : CleanupBehavior
        {
            public TruncateBehavior()
                : base("truncate")
            {
            }

            public override void Cleanup(IPath file)
            {
                using (file.Open(FileMode.Truncate))
                {
                    // Close immediately
                }
            }
        }

        private class DeleteBehavior : CleanupBehavior
        {
            public DeleteBehavior()
                : base("delete")
            {
            }

            public override void Cleanup(IPath file)
            {
                File.Delete(file.ToString());
            }
        }

        private class RecreateBehavior : CleanupBehavior
        {
            public RecreateBehavior()
                : base("recreate")
            {
            }

            public override void Cleanup(IPath file)
            {
                var str = file.ToString();
                File.Delete(str);
                using (File.Create(str))
                {
                    // Close immediately
                }
            }
        }
    }
}
