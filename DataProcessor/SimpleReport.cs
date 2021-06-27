using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessor
{
    public class SimpleReport
    {
        private string pgSchema;
        private string pgConnection;

        public SimpleReport(string schemaName, string connectionStr)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;

        }

        public bool Print()
        {
            Console.WriteLine("Report to print " + DateTime.Now.ToString());
            return true;
        }

    }
}
