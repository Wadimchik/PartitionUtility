using System;
using System.IO;

namespace PartitionUtility
{
    public abstract class LogBase
    {
        protected readonly object lockObj = new object();

        public abstract void Log(LogMessage logMessage);
    }

    public class FileLogger : LogBase
    {
        private string LogPath
        {
            get => Path.Combine(MainWindowVM.LogDirectory, "PartitionUtility_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt");
        }

        public string LogDirectory
        {
            get => Path.GetDirectoryName(LogPath);
        }

        public override void Log(LogMessage logMessage)
        {
            lock (lockObj)
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                // Запись в файл
                using (StreamWriter streamWriter = new StreamWriter(LogPath, true))
                {
                    streamWriter.WriteLine("{0,-26}{1, -10}{2}", logMessage.Time, logMessage.Status, logMessage.Message);
                    streamWriter.Close();
                }
            }
        }
    }
}