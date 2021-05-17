using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCSV
{
    public class Logger
    {
        public const int INFO = 0;
        public const int WARNING = 1;
        public const int ERROR = 2;

        public static void Write(string programName, string stepName, string msg, int severity)
        {
            //to do
            Console.WriteLine($"{programName} - {stepName} \t {GetText(severity)} \t {msg}");
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
        public static void WriteInfo(string programName, string stepName, string msg)
        {
            Write(programName, stepName, msg, INFO);
        }
        public static void WriteEx(string programName, string stepName, Exception ex)
        {
            Write(programName, stepName, ex.Message, ERROR);
            Write(programName, stepName, ex.StackTrace, ERROR);
            Exception innr = ex.InnerException;
            while(innr != null)
            {
                Write(programName, stepName, innr.Message, ERROR);
                Write(programName, stepName, innr.StackTrace, ERROR);

                innr = innr.InnerException;
            }
        }

    }
}
