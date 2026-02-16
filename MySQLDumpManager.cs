using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MySqlConnector;

namespace MySQLDumper
{
    /// <summary>
    /// Manages the creation and maintenance of MySQL database dumps, including backup file management and tracking of
    /// dumped records.
    /// </summary>
    /// <remarks>This class initializes dump settings, performs MySQL dump operations, and handles cleanup of
    /// old backup files. It ensures that only new QSO records are included in each dump and maintains a record of the
    /// last dumped QSO ID. Use this class to automate database backup processes and manage backup retention.</remarks>
    internal class MySQLDumpManager
    {
        private readonly string _CRLF = Environment.NewLine;
        private LogHelper _LogHelper = new LogHelper("MySQLDumper", "MySQLDumper");

        static void Main(string[] args)
        {
            _ = new MySQLDumpManager().MySQLDumpMaintenance();
        }


        /// <summary>
        /// Performs maintenance operations for MySQL dump files, including creating new dumps when records are
        /// available and managing backup files.
        /// </summary>
        /// <remarks>This method checks for new QSO records to determine if a new dump is required. If so,
        /// it creates a new dump file, logs the results, updates the last dumped record ID, and deletes older backup
        /// files if the maximum backup limit is exceeded. If no new records are found, it logs that no dump was
        /// performed. Any exceptions encountered during the process are logged. This method is intended for use in
        /// scenarios where regular maintenance of MySQL dump files and backups is required.</remarks>
        /// <returns>The current instance of the MySQLDumpManager, enabling method chaining.</returns>
        protected MySQLDumpManager MySQLDumpMaintenance()
        {
            try
            {
                DumpManagerSettings settings = new DumpManagerSettings(_LogHelper);
                settings.ThrowIfNull(() => new Exception("DumpManagerSettings failed to initialize and is null."));

                long maxQsoId = GetMaxQsoId(settings);
                long lastDumpedQsoId = GetLastDumpedQsoId(settings);

                // Only perform a dump if there are new QSO records since the last dump.
                if (maxQsoId > lastDumpedQsoId)
                {

                    if (File.Exists(settings.OutputFileFullName))
                    {
                        File.Delete(settings.OutputFileFullName);
                    }

                    // Time the dump operation and invoke the extracted method that performs the mysqldump.

                    Stopwatch sw = Stopwatch.StartNew();
                    (long linesWritten, long bytesWritten) = PerformMySQLDump(settings);
                    sw.Stop();

                    string elapsedFormatted = sw.FormatElapsedTime_mmsssss();
                    _LogHelper.LogMessage($@"mysqldump attempt completed.{_CRLF}File written to '{settings.OutputFileFullName}'.{_CRLF}Lines written: {linesWritten}{_CRLF}Bytes written: {bytesWritten}.{_CRLF}Elapsed time: {elapsedFormatted}");

                    SaveLastDumpedQsoId(settings, maxQsoId);

                    // Look for stuff
                    foreach (FileInfo fileInfo in Directory.GetFiles(settings.BackupDirectory, settings.BackupFileMask).Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).Skip(settings.MaxBackups))
                    {
                        string deleteMessage = $@"Deleted file '{fileInfo.FullName}'.{_CRLF}Last Written: {fileInfo.LastWriteTime}{_CRLF}Create Date:  {fileInfo.CreationTime}{_CRLF}Size:         {fileInfo.Length}";
                        fileInfo.Delete();
                        _LogHelper.LogMessage(deleteMessage);
                    }
                }
                else
                {
                    _LogHelper.LogMessage($"No new QSO records to dump. Max QSO ID: {maxQsoId}, Last Dumped QSO ID: {lastDumpedQsoId}.");
                }
            }
            catch (Exception ex)
            {
                _LogHelper.LogException(ex);
#if DEBUG
                Console.WriteLine("\n\nPress any key to exit.");
                Console.ReadKey();
#endif
            }
            return this;
        }


        /// <summary>
        /// Saves the specified QSO ID as the last dumped identifier to a file in the backup directory.
        /// </summary>
        /// <remarks>If the operation fails, an error message and exception details are logged. The file
        /// will be created or overwritten as needed.</remarks>
        /// <param name="settings">The settings that specify the backup directory and the filename for storing the last dumped QSO ID.</param>
        /// <param name="maxQsoId">The QSO ID to be saved as the most recently dumped identifier.</param>
        private void SaveLastDumpedQsoId(DumpManagerSettings settings, long maxQsoId)
        {
            try
            {
                File.WriteAllText(Path.Combine(settings.BackupDirectory, settings.LastDumpedIDFile), maxQsoId.ToString());
            }
            catch (Exception ex)
            {
                _LogHelper.LogMessage($"Failed to save last dumped QSO ID to '{Path.Combine(settings.BackupDirectory, settings.LastDumpedIDFile)}'.");
                _LogHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Retrieves the last dumped QSO ID from the backup directory specified in the provided settings.
        /// </summary>
        /// <remarks>If the file containing the last dumped QSO ID is missing or contains invalid data,
        /// this method returns 0. Errors encountered during file access are logged and also result in a return value of
        /// 0.</remarks>
        /// <param name="settings">The settings that specify the backup directory and the file name containing the last dumped QSO ID. Cannot
        /// be null.</param>
        /// <returns>The last dumped QSO ID as a long integer. Returns 0 if the file does not exist, contains invalid data, or an
        /// error occurs while reading the file.</returns>
        private long GetLastDumpedQsoId(DumpManagerSettings settings)
        {
            string lastDumpedIdFile = Path.Combine(settings.BackupDirectory, settings.LastDumpedIDFile);
            if (File.Exists(lastDumpedIdFile))
            {
                try
                {
                    if (long.TryParse(File.ReadAllText(lastDumpedIdFile).Trim(), out long lastDumpedQsoId))
                    {
                        return lastDumpedQsoId;
                    }
                    else
                    {
                        _LogHelper.LogMessage($"Content of '{lastDumpedIdFile}' is not a valid long. Defaulting to 0.");
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    _LogHelper.LogException(ex);
                    return 0;
                }
            }
            return 0;
        }


        /// <summary>
        /// Retrieves the maximum QSO ID from the log table in the MySQL database.
        /// </summary>
        /// <remarks>This method connects to a MySQL database and executes a query to find the maximum QSO
        /// ID. It handles potential exceptions related to database connectivity and data conversion, logging errors as
        /// necessary.</remarks>
        /// <param name="settings">The settings used to establish the connection to the MySQL database, including the connection string.</param>
        /// <returns>The maximum QSO ID as a long integer. Returns 0 if no QSO ID is found or if an error occurs during the
        /// database operation.</returns>
        private long GetMaxQsoId(DumpManagerSettings settings)
        {
            long qsoid = 0;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(settings.MySQLConnection))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT MAX(qsoid) FROM log;", conn))
                    {
                        // avoid long waits if the server is unresponsive
                        cmd.CommandTimeout = 30;

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            // Convert robustly: Convert.ToInt64 handles boxed ints, longs, decimals, strings of numbers
                            try
                            {
                                qsoid = Convert.ToInt64(result);
                            }
                            catch (FormatException)
                            {
                                // fallback: try parsing string form
                                if (long.TryParse(result.ToString(), out long parsed))
                                {
                                    qsoid = parsed;
                                }
                                else
                                {
                                    _LogHelper.LogMessage($"Unable to convert MAX(qsoid) result '{result}' to Int64.");
                                }
                            }
                            catch (OverflowException)
                            {
                                _LogHelper.LogMessage($"MAX(qsoid) value '{result}' is outside the range of Int64.");
                                qsoid = result is IConvertible ? (long)(result is decimal dec ? decimal.Truncate((decimal)result) : long.MaxValue) : long.MaxValue;
                            }
                        }
                    }
                }
            }
            catch (MySqlException mex)
            {
                // DB-level errors (auth, network, syntax)
                _LogHelper.LogException(mex);
            }
            catch (InvalidOperationException ioex)
            {
                // connection state/usage errors
                _LogHelper.LogException(ioex);
            }
            catch (Exception ex)
            {
                // catch-all so app doesn't crash unexpectedly
                _LogHelper.LogException(ex);
            }

            return qsoid;
        }


        /// <summary>
        /// Executes a MySQL dump using the specified settings and writes the output to a file, returning the number of
        /// lines and bytes written.
        /// </summary>
        /// <remarks>The method waits for up to 5 minutes for the dump process to complete and ensures that
        /// all buffered data is flushed to the output file before returning.</remarks>
        /// <param name="settings">The settings that configure the dump operation, including the command file name, command arguments, and the
        /// output file path.</param>
        /// <returns>A tuple containing the number of lines written and the total number of bytes written to the output file.</returns>
        private (long linesWritten, long bytesWritten) PerformMySQLDump(DumpManagerSettings settings)
        {
            long bytesWritten = 0;
            long linesWritten = 0;

            using (Process process = new Process())
            {
                using (FileStream sqlDumpFile = new FileStream(settings.OutputFileFullName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sqlStreamWriter = new StreamWriter(sqlDumpFile))
                {
                    process.StartInfo.FileName = settings.CommandFileName;
                    process.StartInfo.WorkingDirectory = "";
                    process.StartInfo.Arguments = settings.CommandArguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        // Prepend line numbers to each line of the output.
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            sqlStreamWriter.WriteLine(e.Data);
                            bytesWritten += e.Data.Length;
                            linesWritten++;
                        }
                    });

                    process.Start();

                    // Asynchronously read the standard output of the spawned process.
                    // This raises OutputDataReceived events for each line of output.
                    process.BeginOutputReadLine();
                    process.WaitForExit(5 * 60 * 1000);    // Wait for 5 minutes, tops;

                    // Ensure buffered data is written out.
                    sqlStreamWriter.Flush();
                    sqlDumpFile.Flush();
                }
            }

            return (linesWritten, bytesWritten);
        }
    }
}
