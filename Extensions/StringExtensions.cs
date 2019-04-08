using System;

namespace Extensions
{
    public static class StringExtensions
    {
        #region Constants

        private const char _DoubleQuote = '\"';

        #endregion

        #region Methods

        public static bool IsNullOrWhitespace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        public static string Quote(this string source)
        {
            return source.Quote(_DoubleQuote);
        }

        public static string Quote(this string source, char quoteCharacter)
        {
            return $"{quoteCharacter}{source}{quoteCharacter}";
        }

        public static string Truncate(this string source, int length)
        {
            return source.Substring(0, Math.Min(length, source.Length - 1));
        }

        #endregion
    }
}