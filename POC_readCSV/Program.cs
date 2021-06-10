using System;
using ReadCSV;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace POC_readCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            string pgConStr = configuration["pgConnStr"];
            string pgSchema = configuration["pgSchema"];
            string jsonFileDef = configuration["jsonFileDef"];
            string theDelim = configuration["DelimterChar"];
            char[] delims = theDelim.ToCharArray();
            string logFileName = configuration["logFileName"];
            Logger.SetLogFileName(logFileName);

            //FtpUtil.DownloadDir();
            //FtpUtil.JsonTest();
            //TestFilePart(pgConStr, pgSchema);

            //string testInputFilePath = @"C:\d\Personal\Ventura\Sample file_15052021\Sample _Files _15052021\NPS_regular\Input_file_NPS_regular\PTGCHG0507202114070521002.TXT";
            string testInputFilePath = @"C:\zunk\PTGCHG0507202114070521002.TXT";
            int jobId = 0;
            Util.SaveInputToDB(pgConStr, pgSchema, jobId, testInputFilePath, jsonFileDef, delims[0]);
        }

        private static void TestFilePart(string pgConStr, string pgSchema)
        {
            UtilCSV util = new UtilCSV();
            bool isReadOk = util.ReadCSV(pgConStr, pgSchema);
            if (isReadOk)
            {
                string nowStr = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");

                Console.WriteLine("Done reading " + nowStr);
            }
            else
            {
                Console.WriteLine("See error log");
            }
        }
    }
}
