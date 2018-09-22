using System;
using System.IO;

//created marenin dl 01042018

namespace MP_FR_Command
{
    class Log
    {
        public static void Write(string strLog, int levelOutput=0)
        {
            
            if (levelOutput == -1)
            {
                return;
            }

            var dateTimeNow = DateTime.Now;

            if (levelOutput == 0)
            {
                string logFilePath = AppDomain.CurrentDomain.BaseDirectory;

                logFilePath = logFilePath + "\\log-" + System.DateTime.Now.ToString("MM-dd-yyyy HH") + "." + "txt";

                FileInfo logFileInfo = new FileInfo(logFilePath);

                DirectoryInfo logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists)
                {
                    logDirInfo.Create();
                }

                FileStream fileStream = null;
                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }

                StreamWriter logStream = new StreamWriter(fileStream);
                logStream.WriteLine(dateTimeNow.ToString() + " - " + strLog);
                logStream.Close();

                logStream = null;

                fileStream = null;
            }
            else if (levelOutput == 1)
            {
                Console.WriteLine(dateTimeNow.ToString() + " - " + strLog);
            }
        }
    }
}