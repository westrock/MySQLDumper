using System;
using System.Diagnostics;

namespace MySQLDumper
{
    /// <summary>
    /// Provides extension methods including validating string and object values, throwing exceptions if the values are null
    /// or whitespace.
    /// </summary>
    public static class ClassExtensions
    {
        /// <summary>
        /// Throws an exception if the specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <remarks>This method is useful for validating input parameters to ensure they meet required
        /// conditions before proceeding with further processing.</remarks>
        /// <param name="value">The string to validate. This parameter cannot be null, empty, or consist only of white-space characters.</param>
        /// <param name="message">The message to include in the exception if the validation fails.</param>
        /// <exception cref="Exception">Thrown if <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
        public static void ThrowIfNullOrWhitespace(this string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception(message);
            }
        }


        /// <summary>
        /// Throws an exception if the specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <remarks>This method is typically used to enforce non-null and non-empty string parameters in
        /// method signatures.</remarks>
        /// <param name="value">The string to validate. This parameter cannot be null or white-space; otherwise, an exception is thrown.</param>
        /// <param name="exceptionFactory">A factory function that creates the exception to throw when the validation fails. This parameter cannot be
        /// null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exceptionFactory"/> is null.</exception>
        public static void ThrowIfNullOrWhiteSpace(this string value, Func<Exception> exceptionFactory)
        {
            if (exceptionFactory == null)
            {
                throw new ArgumentNullException(nameof(exceptionFactory));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw exceptionFactory();
            }
        }


        /// <summary>
        /// Throws a custom exception if the specified object is null.
        /// </summary>
        /// <remarks>Use this method to validate that an object is not null and to throw a specific
        /// exception if the validation fails. This is useful for enforcing preconditions with custom error
        /// handling.</remarks>
        /// <param name="value">The object to check for null. If this parameter is null, the exception provided by <paramref
        /// name="exceptionFactory"/> is thrown.</param>
        /// <param name="exceptionFactory">A function that returns the exception to throw if <paramref name="value"/> is null. This parameter cannot be
        /// null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exceptionFactory"/> is null.</exception>
        public static void ThrowIfNull(this object value, Func<Exception> exceptionFactory)
        {
            if (exceptionFactory == null)
            {
                throw new ArgumentNullException(nameof(exceptionFactory));
            }

            if (value == null)
            {
                throw exceptionFactory();
            }
        }

        public static string FormatElapsedTime_mmsssss(this Stopwatch stopwatch)
        {
            TimeSpan elapsed = stopwatch.Elapsed;
            return $"{(long)elapsed.TotalMinutes}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";
        }
    }
}
