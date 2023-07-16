using System;

namespace MySQLDumper
{
    public static class ClassExtensions
    {
        public static void ThrowIfNullOrWhitespace(this string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception(message);
            }
        }
    }
}
