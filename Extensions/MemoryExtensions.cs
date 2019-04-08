using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class MemoryExtensions
    {
        private static readonly List<string> ByteNames = new List<string> {"Bytes", "KB", "MB", "GB"};

        public static string ToFileSize(this int source)
        {
            return Convert.ToInt64(source).ToFileSize();
        }

        public static string ToFileSize(this long source)
        {
            const int byteConversion = 1024;
            var bytes = Math.Abs(Convert.ToDouble(source));
            var prefix = source < 0 ? "-" : "";
            for (var i = 4; i >= 0; i--)
            {
                var converted = Math.Pow(byteConversion, i);
                if (bytes >= converted) return $"{prefix}{Math.Round(bytes / converted, 2)} {ByteNames[i]}";
            }

            return "Unknown";
        }
    }
}