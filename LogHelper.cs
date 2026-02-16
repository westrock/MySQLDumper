using System;
using System.Diagnostics;
using System.Security;

namespace MySQLDumper
{
    public class LogHelper
    {
        public string ApplicationName { get; protected set; }
        public string EventSource { get; protected set; }
        public string LogName { get; protected set; }
        private EventLog _EventLog { get; set; }

        #region Constructor(s)
        public LogHelper(string applicationName, string logName)
        {
            ApplicationName = applicationName;
            LogName = logName;

            EventSource = CreateEventSource(applicationName, logName);
            _EventLog = new EventLog(applicationName);
            _EventLog.Source = EventSource;
        }

        #endregion

        #region Public Methods

        public void LogException(Exception ex)
        {
            _EventLog.WriteEntry(ex.Message.Substring(0, Math.Min(ex.Message.Length, 32766)), EventLogEntryType.Information);
            Console.WriteLine(ex.Message.Substring(0, Math.Min(ex.Message.Length, 32766)), EventLogEntryType.Information);
        }

        public void LogMessage(string message)
        {
            _EventLog.WriteEntry(message.Substring(0, Math.Min(message.Length, 32766)), EventLogEntryType.Information);
            Console.WriteLine(message.Substring(0, Math.Min(message.Length, 32766)), EventLogEntryType.Information);
        }

        #endregion



        #region Private Methods

        private static string CreateEventSource(string currentAppName, string logName)
        {
            string eventSource = currentAppName;

            try
            {
                // searching the source throws a security exception ONLY if not exists!

                if (!EventLog.SourceExists(eventSource))
                {
                    EventLog.CreateEventSource(eventSource, logName);
                }
            }
            catch (SecurityException)
            {
                eventSource = "Application";
            }

            return eventSource;
        }

        #endregion

    }
}
