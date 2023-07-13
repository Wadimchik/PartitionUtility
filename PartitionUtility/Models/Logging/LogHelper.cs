using System;

namespace PartitionUtility
{
    /// <summary>
    /// Прослойка над логгерами разных типов
    /// </summary>
    public static class LogHelper
    {
        public class Status
        {
            public const string Ok = "Ok";
            public const string Warning = "Warning";
            public const string Info = "Info";
            public const string User = "User";
            public const string Debug = "Debug";
            public const string Error = "Error";
            public const string CriticalError = "Critical";
        }

        // text properties of log's content
        private static LogBase FileLog = new FileLogger();

        private static string GetDate()
        {
            return DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff");
        }

        /// <summary>
        /// Производит запись во все журналы
        /// </summary>
        public static void Log(string logStatus, string message)
        {
            LogMessage logMessage = new LogMessage(logStatus, LogHelper.GetDate(), message);

            FileLog.Log(logMessage);
        }

        /// <summary>
        /// Производит запись ошибки во все журналы. Производит форматирвоание указанного сообщения и текста ошибки
        /// </summary>
        public static void Log(string logStatus, string message, Exception exception)
        {
            //LogMessage logMessageUI = new LogMessage(logStatus, LogHelper.GetDate(), message + ": \"" + exception.Message + "\"");
            
            #if DEBUG
            LogMessage logMessageError = new LogMessage(logStatus, LogHelper.GetDate(), message + ": \"" + exception.ToString() + "\"");
            #else
            LogMessage logMessageError = new LogMessage(logStatus, LogHelper.GetDate(), message + ": \"" + exception.Message + "\"");
            #endif

            FileLog.Log(logMessageError);
        }
    }
}