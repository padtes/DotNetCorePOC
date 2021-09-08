using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging
{
    public class Logger
    {
        private static string _fileName;
        //private static int logLevel;

        public const int VERBOSE = 0;
        public const int INFO = 1;
        public const int WARNING = 2;
        public const int ERROR = 3;

        private static object syncLock = new object();
        private static bool fileLogOff = false;
        private static List<string> msgQue = new List<string>();

        public static void SetLogFileName(string fileName)
        {
            _fileName = fileName;

            if (string.IsNullOrEmpty(_fileName))
            {
                throw new Exception("LogFileName NOT FOUND in app.config, aborting");
            }
            if (_fileName.EndsWith(".txt"))
            {
                _fileName = _fileName.Substring(0, _fileName.Length - 4);
            }
            _fileName = _fileName + "_" + DateTime.Now.ToString("yyMMdd") + ".txt";
        }

        public static void StopFileLog()
        {
            if (Debugger.IsAttached == false)
                fileLogOff = true;
        }
        public static void StartFileLog()
        {
            lock (syncLock)
            {
                fileLogOff = false;

                while (msgQue.Count > 0)
                {
                    string logLine = msgQue[0];
                    WriteFileLine(logLine);
                    msgQue.RemoveAt(0);
                }
            }
        }

        public static void Write(string programName, string stepName, int jobId, string msg, int severity)
        {
            //to do
            //use database log table
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {GetText(severity)} \t {programName} - {stepName} job# {jobId}\t{msg}");
            //to do
            //remove file write after database write is done
            WriteToFile(programName, stepName, jobId, msg, severity);
        }

        private static void WriteToFile(string programName, string stepName, int jobId, string msg, int severity)
        {
            string logLine = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {GetText(severity)} \t {programName} - {stepName} job# {jobId}\t{msg}";

            if (fileLogOff)
            {
                msgQue.Add(logLine);
                return;
            }

            WriteFileLine(logLine);
        }

        private static void WriteFileLine(string logLine)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_fileName, true))
                {
                    sw.WriteLine(logLine);
                }
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        private static string GetText(int severity)
        {
            if (severity == INFO)
                return "INFO";
            if (severity == WARNING)
                return "WARNING";
            if (severity == ERROR)
                return "ERROR";
            return "VERBOSE";
        }
        public static void WriteInfo(string programName, string stepName, int jobId, string msg)
        {
            Write(programName, stepName, jobId, msg, INFO);
        }
        public static void WriteEx(string programName, string stepName, int jobId, Exception ex)
        {
            Write(programName, stepName, jobId, ex.Message, ERROR);
            Write(programName, stepName, jobId, ex.StackTrace, ERROR);

            Exception innr = ex.InnerException;
            int i = 1;
            while (innr != null)
            {
                Write(programName, stepName, jobId, i + ":" + innr.Message, ERROR);
                Write(programName, stepName, jobId, i + ":" + innr.StackTrace, ERROR);

                innr = innr.InnerException;
                i++;
            }
        }

    }
}
