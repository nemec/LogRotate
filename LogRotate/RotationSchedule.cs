using System;
using System.ComponentModel;

namespace LogRotate
{
    [TypeConverter(typeof(EnumerationTypeConverter<RotationSchedule>))]
    public abstract class RotationSchedule : Enumeration<RotationSchedule>
    {
        public static RotationSchedule 
            SizeOnly = new SizeOnlyType(0, "sizeonly"),
            Daily = new DailyType(1, "daily"),
            Weekly = new WeeklyType(2, "weekly"),
            Monthly = new MonthlyType(3, "monthly");


        protected static Func<DateTime> ReferenceTime = () => DateTime.UtcNow;

        protected RotationSchedule(int value, string displayName)
            : base(value, displayName)
        {
        }

        public abstract bool ShouldRotate(DateTime fileAge);

        public static void SetReferenceTime(Func<DateTime> referenceTime)
        {
            ReferenceTime = referenceTime;
        }

        private class DailyType : RotationSchedule
        {
            public DailyType(int value, string displayName)
                : base(value, displayName)
            {
            }

            public override bool ShouldRotate(DateTime fileAge)
            {
                var reference = ReferenceTime();

                // Truncate day
                reference = new DateTime(
                    reference.Year, reference.Month, reference.Day);
                fileAge = new DateTime(
                    fileAge.Year, fileAge.Month, fileAge.Day);

                return reference > fileAge;
            }
        }

        private class MonthlyType : RotationSchedule
        {
            public MonthlyType(int value, string displayName)
                : base(value, displayName)
            {
            }

            public override bool ShouldRotate(DateTime fileAge)
            {
                DateTime reference = ReferenceTime();

                // Truncate month
                reference = new DateTime(reference.Year, reference.Month, 1);
                fileAge = new DateTime(fileAge.Year, fileAge.Month, 1);

                return reference > fileAge;
            }
        }

        private class SizeOnlyType : RotationSchedule
        {
            public SizeOnlyType(int value, string displayName)
                : base(value, displayName)
            {
            }

            public override bool ShouldRotate(DateTime fileAge)
            {
                return false;
            }
        }

        private class WeeklyType : RotationSchedule
        {
            public WeeklyType(int value, string displayName)
                : base(value, displayName)
            {
            }

            public override bool ShouldRotate(DateTime fileAge)
            {
                DateTime reference = ReferenceTime();

                // Truncate week
                reference = new DateTime(
                    reference.Year, reference.Month, reference.Day) -
                            TimeSpan.FromDays((int) reference.DayOfWeek);
                fileAge = new DateTime(
                    fileAge.Year, fileAge.Month, fileAge.Day) -
                          TimeSpan.FromDays((int) reference.DayOfWeek);

                return reference > fileAge;
            }
        }
    }
}