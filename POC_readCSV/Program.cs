using System;
using ReadCSV;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Logging;
using WriteWord;
using WriteDocXML;
using NpsScriban;

namespace POC_readCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            TestScriban();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            string pgConnection = configuration["pgConnStr"];
            string pgSchema = configuration["pgSchema"];
            string jsonFileDef = configuration["jsonFileDef"];
            string theDelim = configuration["DelimterChar"];
            char[] delims = theDelim.ToCharArray();
            string logFileName = configuration["logFileName"];
            Logger.SetLogFileName(logFileName);

            //FtpUtil.DownloadDir();
            //FtpUtil.JsonTest();
            //TestFilePart(pgConnection, pgSchema);

            //string testInputFilePath = @"C:\d\Personal\Ventura\Sample file_15052021\Sample _Files _15052021\NPS_regular\Input_file_NPS_regular\PTGCHG0507202114070521002.TXT";
            string testInputFilePath = @"C:\zunk\PTGCHG0507202114070521002.TXT";
            int jobId = 0;

            bool testingRead = false;
            bool testWord = true;
            string bizType = "POC";

            if (testingRead)
                Util.SaveInputToDB(pgConnection, pgSchema, bizType, jobId, testInputFilePath, jsonFileDef, delims[0]);
            else
            {
                //WordUtil wutil = new WordUtil(@"C:\d\zunk\testDocx\test1teplate.docx", @"C:\d\zunk\testDocx\20210610_out\");
                //wutil.GetCopyForIdTest("mitwa.Docx", "{{first_name}}", "Mitawa");
                //wutil.GetCopyForIdTest("friend.Docx", "{{first_name}}", "friend");

                if (testWord)
                    TestXmlReplacement(args);
                else
                {
                    string[] testArgs = new string[] { "14070521002", "PRF","2021/06/21" };
                    TestWriteCSV(testArgs, 0);
                }

            }
        }

        private static void TestScriban()
        {
            ScribanTest.Test();
        }

        private static void TestWriteCSV(string[] args, int jobId)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();
            string pgConnection = configuration["pgConnStr"];
            string pgSchema = configuration["pgSchema"];
            string jsonCsvDef = configuration["jsonCsvDef"];

            string outputDir = @"C:\d\zunk\testDocx\20210610_out\";
            string fileName = "POC_PTC_NPS_APY.txt";
            string bizType = "POCwrite";
            CsvUtil csv = new CsvUtil(pgConnection, pgSchema, bizType, jsonCsvDef, outputDir);
            csv.CreateFile(bizType, fileName, args, jobId);
        }

        private static void TestXmlReplacement(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();
            string pgConnection = configuration["pgConnStr"];
            string pgSchema = configuration["pgSchema"];
            string jsonLetterDef = configuration["jsonLetterDef"];

            //read from DB -- Open: what is "Where".
            //to do form select
            //to do execute select
            //to do c-functions support ?? only sql function
            //to do get List<KeyValuePair<string, string>> for each row

            //DocxUtil dox = new DocxUtil(pgConnection, pgSchema, jsonLetterDef, @"C:\d\zunk\testDocx\test_unzip\test1Teplate\word\document_big.xml", @"C:\d\zunk\testDocx\20210610_out\");
            //dox.CreateAllSeparateFiles(0);
            //dox.CreateMultiPageFiles(0, 500);

            DocxUtil dox = new DocxUtil(pgConnection, pgSchema, jsonLetterDef, @"C:\d\zunk\testDocx\test_unzip\NPS_APY_LETTER_Single\word\document.xml", @"C:\d\zunk\testDocx\20210610_out\");

            dox.CreateMultiPageFiles("lite", 0, 3, args);

            //List<KeyValuePair<string, string>> tokenMap1 = new List<KeyValuePair<string, string>>();
            //tokenMap1.Add(new KeyValuePair<string, string>("{{first_name}}", "My Mate"));
            //tokenMap1.Add(new KeyValuePair<string, string>("{{last_name}}", "Biggus"));
            //dox.CreateFile("mitwa.xml", tokenMap1);

            //tokenMap1.Clear();
            //tokenMap1.Add(new KeyValuePair<string, string>("{{first_name}}", "Your Friend"));
            //tokenMap1.Add(new KeyValuePair<string, string>("{{last_name}}", "Diggus"));
            //dox.CreateFile("friend.xml", tokenMap1);
        }

        private static void TestFilePart(string pgConnection, string pgSchema)
        {
            UtilCSV util = new UtilCSV();
            bool isReadOk = util.ReadCSV(pgConnection, pgSchema);
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
