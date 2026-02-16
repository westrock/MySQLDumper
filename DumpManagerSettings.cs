// Pseudocode / Plan:
// 1. Replace the manual null-or-whitespace checks that throw ArgumentException with calls
//    to the new string extension method `ThrowIfNullOrWhiteSpace` so the checks are concise
//    and preserve the original exception messages.
// 2. For each required AppSettings string value:
//      - Call `value = value.ThrowIfNullOrWhiteSpace("Missing or empty AppSettings key \"KeyName\"");`
//      - This both validates and returns the original string for assignment.
// 3. Keep all other logic (parsing MaxBackups, OutputFile token replacement, property assignment)
//    unchanged and compatible with C# 7.3 / .NET Framework 4.8.
// 4. Do not introduce any new helper methods; rely on the provided extension method.
// 5. Emit the full updated file with the above changes.

// Note: This file assumes an extension method with signature similar to:
//       public static string ThrowIfNullOrWhiteSpace(this string value, string message)
// which throws ArgumentException with the provided message when `value` is null/whitespace
// and returns `value` otherwise.

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace MySQLDumper
{
    /// <summary>
    /// Provides configuration settings for the Dump Manager, including command file details and backup options.
    /// </summary>
    /// <remarks>This class initializes its properties from the provided NameValueCollection, typically
    /// sourced from application settings. It ensures that all required settings are present and valid, throwing
    /// exceptions if any are missing or empty. The MaxBackups property defaults to 14 if the corresponding app
    /// setting cannot be parsed as an integer.</remarks>
    internal class DumpManagerSettings
    {
        private string outputFileFullName = null;
        // Get-only properties
        public string MySQLConnection { get; }
        public string CommandFileName { get; }
        public string CommandArguments { get; }
        public string OutputFile { get; }
        public string OutputFileFullName
        {
            get
            {
                if (outputFileFullName == null)
                {
                    outputFileFullName = Path.Combine(BackupDirectory, OutputFile);
                }
                return outputFileFullName;
            }
        }
        public string BackupFileMask { get; }
        public string BackupDirectory { get; }
        public string MaxBackupsString { get; }
        public string LastDumpedIDFile { get; }
        public int MaxBackups { get; }

        private LogHelper _LogHelper = null;

        private const int DefaultMaxBackups = 14;


        /// <summary>
        /// Initializes a new instance of the DumpManagerSettings class using application settings from the
        /// configuration file.
        /// </summary>
        /// <param name="logHelper">An instance of LogHelper for logging messages and exceptions.</param>
        /// <remarks>This constructor loads settings from ConfigurationManager.AppSettings, allowing
        /// configuration to be managed through the application's configuration file. Use this constructor when you want
        /// to initialize DumpManagerSettings with values defined in the application's configuration.</remarks>
        public DumpManagerSettings(LogHelper logHelper)
            : this(logHelper, ConfigurationManager.AppSettings)
        {
        }


        /// <summary>
        /// Initializes a new instance of the DumpManagerSettings class using the specified application settings.
        /// </summary>
        /// <remarks>The constructor reads required values from the provided NameValueCollection and
        /// validates that all necessary settings are present. If MaxBackups cannot be parsed as an integer, a
        /// default value of 14 days is used. If the OutputFile setting contains a {DateTime} token, it is replaced with
        /// the current date and time.</remarks>
        /// <param name="_LogHelper">An instance of LogHelper for logging messages and exceptions.</param>
        /// <param name="appSettings">A collection of application settings that must include valid values for CommandFileName, CommandArguments,
        /// OutputFile, BackupFileMask, BackupDirectory, and MaxBackups.</param>
        /// <exception cref="ArgumentNullException">Thrown if the appSettings parameter is null.</exception>
        public DumpManagerSettings(LogHelper logHelper, NameValueCollection appSettings)
        {
            _LogHelper = logHelper;
            if (appSettings == null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }

            // Read raw values
            string mySQLConnection = appSettings["MySQLConnection"];
            string commandFileName = appSettings["CommandFileName"];
            string commandArguments = appSettings["CommandArguments"];
            string outputFile = appSettings["OutputFile"];
            string backupFileMask = appSettings["BackupFileMask"];
            string backupDirectory = appSettings["BackupDirectory"];
            string maxBackupsString = appSettings["MaxBackups"];
            string lastDumpedIDFile = appSettings["LastDumpedIDFile"];

            // Validate required strings using the new extension method.
            // Preserves original exception messages.
            mySQLConnection.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"MySQLConnection\""));
            commandFileName.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"CommandFileName\""));
            commandArguments.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"CommandArguments\""));
            outputFile.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"OutputFile\""));
            backupFileMask.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"BackupFileMask\""));
            backupDirectory.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"BackupDirectory\""));
            lastDumpedIDFile.ThrowIfNullOrWhiteSpace(() => new Exception("Missing or empty AppSettings key \"LastDumpedIDFile\""));

            // Parse MaxBackups if possible
            if (!int.TryParse(maxBackupsString, out int maxBackups))
            {
                // If parsing fails, set default to null. The caller can inspect MaxBackupsString if needed.
                maxBackups = DefaultMaxBackups;

                _LogHelper.LogMessage($"AppSettings key \"MaxBackups\" value '{maxBackupsString}' is not an int.  Using default of {DefaultMaxBackups}.");
            }

            // Replace {DateTime} token in OutputFile
            if (!string.IsNullOrEmpty(outputFile))
            {
                outputFile = outputFile.Replace("{DateTime}", $"{DateTime.Now:yyyyMMdd_hhmmss}");
            }

            // Assign to read-only properties
            MySQLConnection = mySQLConnection;
            CommandFileName = commandFileName;
            CommandArguments = commandArguments;
            OutputFile = outputFile;
            BackupFileMask = backupFileMask;
            BackupDirectory = backupDirectory;
            MaxBackups = maxBackups;
            MaxBackupsString = maxBackupsString;
            LastDumpedIDFile = lastDumpedIDFile;
        }
    }
}