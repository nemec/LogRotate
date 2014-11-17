using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogRotate
{
    public static class ByteConverter
    {
        public static readonly Dictionary<string, long> ConversionTable = new Dictionary<string, long>
        {
            {"B", 1L},
            {"KB", 1000L},
            {"MB", 1000000L},
            {"GB", 1000000000L},
            {"KIB", 1024L},
            {"MIB", 1048576L},
            {"GIB", 1073741824L}
        };

        public static long ParseString(string size)
        {
            Match match = Regex.Match(size.ToUpper().Trim(), "(\\d+)\\s*(\\w{1,3})?");
            if (!match.Success)
            {
                throw new FormatException(string.Format("String {0} cannot be converted to bytes.", size));
            }
            long num1 = long.Parse(match.Groups[1].ToString());
            if (!match.Groups[2].Success)
            {
                return num1;
            }
            long num2;
            if (!ConversionTable.TryGetValue(match.Groups[2].ToString().ToUpperInvariant(), out num2))
            {
                throw new FormatException(string.Format(
                    "Cannot convert {0} to a conversion prefix.",
                    match.Groups[2]));
            }
            return num1*num2;
        }
    }
}