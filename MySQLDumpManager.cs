using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace MySQLDumper
{
    internal class MySQLDumpManager
    {
        private LogHelper _LogHelper = new LogHelper("MySQLDumper", "MySQLDumper");

        static void Main(string[] args)
        {
            _ = new MySQLDumpManager().MySQLDumpMaintenance();
        }


        protected MySQLDumpManager MySQLDumpMaintenance()
        {
            try
            {
                int backupAgeDays = 14;
                long bytesWritten = 0;
                long linesWritten = 0;
                string _CommandFileName = ConfigurationManager.AppSettings["CommandFileName"];
                string _CommandArguments = ConfigurationManager.AppSettings["CommandArguments"];
                string _OutputFile = ConfigurationManager.AppSettings["OutputFile"];
                string _BackupFileMask = ConfigurationManager.AppSettings["BackupFileMask"];
                string _BackupDirectory = ConfigurationManager.AppSettings["BackupDirectory"];
                string _BackupAgeDaysString = ConfigurationManager.AppSettings["BackupAgeDays"];

                _CommandFileName.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"CommandFileName\"");
                _CommandArguments.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"CommandArguments\"");
                _OutputFile.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"OutputFile\"");
                _BackupDirectory.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"BackupDirectory\"");
                _BackupFileMask.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"_BackupFileMask\"");
                _BackupAgeDaysString.ThrowIfNullOrWhitespace("Missing or empty AppSettings key \"BackupAgeDays\"");

                if (!int.TryParse(_BackupAgeDaysString, out backupAgeDays))
                {
                    _LogHelper.LogMessage($"AppSettings key \"BackupDaysAge\" value '{_BackupAgeDaysString}' is not an int.  Using default of 14.");
                }

                _OutputFile = _OutputFile.Replace("{DateTime}", $"{DateTime.Now:yyyyMMdd_hhmmss}");

                if (File.Exists(_OutputFile))
                {
                    File.Delete(_OutputFile);
                }

                using (Process process = new Process())
                {
                    using (FileStream sqlDumpFile = new FileStream(_OutputFile, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sqlStreamWriter = new StreamWriter(sqlDumpFile))
                        {
                            process.StartInfo.FileName = _CommandFileName;
                            process.StartInfo.WorkingDirectory = "";
                            process.StartInfo.Arguments = _CommandArguments;
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
                            process.WaitForExit(60 * 60 * 1000);    // Wait for an hour, tops;
                        }
                    }
                }
                // Write the redirected output to this application's window.
                string executionSummary = $@"mysqldump attempt completed.
File written to '{_OutputFile}'.
Lines written: {linesWritten}
Bytes written: {bytesWritten}.";

                _LogHelper.LogMessage(executionSummary);
                Console.WriteLine(executionSummary);

                // Look for stuff

                IEnumerable<string> files = Directory.GetFiles(_BackupDirectory, _BackupFileMask);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    double fileAgeDays = (DateTime.Now - fileInfo.LastWriteTime).TotalDays;

                    if (fileAgeDays > backupAgeDays)
                    {
                        File.Delete(file);
                        _LogHelper.LogMessage($@"Deleted file '{file}'.
Last Written: {fileInfo.LastWriteTime}
Create Date:  {fileInfo.CreationTime}
Size:         {fileInfo.Length}
");
                    }
                }
            }
            catch (Exception ex)
            {
                _LogHelper.LogException(ex);
                Console.WriteLine(ex.ToString());

                Console.WriteLine("\n\nPress any key to exit.");
                Console.ReadKey();
            }
            return this;
        }
    }
}
