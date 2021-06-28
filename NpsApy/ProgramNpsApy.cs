using CommonUtil;
using DbOps;
using Logging;
using DataProcessor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DbOps.Structs;
using System.Text;
using System.IO;
using System.Globalization;
using System.Linq;

namespace NpsApy
{
    class ProgramNpsApy
    {
        static void Main(string[] args)
        {
            TestWrFile();
            //to do validate file based on column def - need to change col def for length, value range, empty ot not

            if (args.Length == 0 || (args.Length == 1 && args[0] == "-help"))
            {
                Console.WriteLine("enter command as ProgramNpsApy -modulename=[Lite|Reg|All] -op=[Read|Write|Report] -runFor=[All|directory name yyyymmdd] -courier=[OPTIONAL courier code(s) as csv For WRITE]");
                Console.WriteLine("for ex. NOTE the DASH");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=Read");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=write -courier=ABC,PQR");
                Console.WriteLine("---- special case ----");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=UNLOCK  : this will unlock Courier Counter lock");
                return;
            }

            string pgSchema, pgConnection, logFileName;

            ReadSystemConfig(out pgSchema, out pgConnection, out logFileName);
            Logger.SetLogFileName(logFileName);

            string paramsMsg = string.Join(' ', args);
            //get runtime parameter
            string bizType;    //valid values ALL | LITE | REG 
            string operation; //Op == Operation: READ or WRITE or REPORT -   All == READ as well as WRITE
            string runFor;    //default - no param - scan base directory and process all
            string courierCSV;

            ParseCommandArgs(args, out bizType, out operation, out runFor, out courierCSV);

            Logger.Write("ProgramNpsApy", "main", 0, "Nps APY Run Success " + string.Join(' ', args), Logger.INFO);

            // read appSettings.json config


            bool runResult;
            if (operation == "unlock")
            {
                bool ok = DbUtil.Unlock(pgSchema, pgConnection);
                if (ok)
                    Console.WriteLine("Unlock done");
                else
                    Console.WriteLine("Unlock FAILED, check exceptions");

                return;            
            }
            if (operation == "report")
            {
                // REPORT will dump simple report of counts by <date>, <LITE | APY | REGULAR>, < COURIER >, count of yet to print cards or in records in error
                SimpleReport rep = new SimpleReport(pgSchema, pgConnection);
                runResult = rep.Print();
            }
            else
            {
                runResult = ProcessData(pgSchema, pgConnection, bizType, operation, runFor, courierCSV);
            }

            if (runResult == false)
            {
                Logger.Write("ProgramNpsApy", "main", 0, "Run Failed " + paramsMsg, Logger.ERROR);
                Console.WriteLine("Run Failed " + paramsMsg);
            }
            else
            {
                Logger.Write("ProgramNpsApy", "main", 0, "Run Success " + paramsMsg, Logger.INFO);
                Console.WriteLine("Run Success " + paramsMsg);
            }

        }

        private static bool ProcessData(string pgSchema, string pgConnection, string bizType, string operation, string runFor, string courierCcsv)
        {
            bool run = false;

            if (bizType == "lite" || bizType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_LITE, pgSchema, pgConnection);

                run = processor.ProcessModule(operation, runFor, courierCcsv);
            }

            if (bizType == "reg" || bizType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_REG, pgSchema, pgConnection);

                run = processor.ProcessModule(operation, runFor, courierCcsv);
            }
            return run;
        }

        private static void ParseCommandArgs(string[] args, out string moduleName, out string operation, out string runFor, out string courierCSV)
        {
            //enter command as ProgramNpsApy -bizType=Lite -op=Read -runFor=ALL -courier=ABC,PQR 

            moduleName = "all";
            operation = "all";
            runFor = "all";
            courierCSV = "";

            Regex cmdRegEx = new Regex(@"-(?<name>.+?)=(?<val>.+)");
            Dictionary<string, string> cmdArgs = new Dictionary<string, string>();

            foreach (string s in args)
            {
                Match m = cmdRegEx.Match(s);
                if (m.Success)
                {
                    string cmdName = m.Groups[1].Value.Trim().ToLower();
                    string cmdVal = m.Groups[2].Value.Trim().ToLower();
                    cmdArgs.Add(cmdName, cmdVal);
                    Console.WriteLine(m.Groups[1].Value + "::" + m.Groups[2].Value);
                }
            }

            if (cmdArgs.ContainsKey("modulename"))
            {
                moduleName = cmdArgs["modulename"]; //ALL | LITE | REG 
            }
            if (cmdArgs.ContainsKey("op"))
            {
                operation = cmdArgs["op"]; //READ or WRITE or REPORT -   All == READ as well as WRITE
            }
            if (cmdArgs.ContainsKey("runfor"))
            {
                runFor = cmdArgs["runfor"]; //date of download
            }
            if (cmdArgs.ContainsKey("courier"))
            {
                courierCSV = cmdArgs["courier"];
            }

            if (!(moduleName == "all" || moduleName == "lite" || moduleName == "reg"))
            {
                throw new Exception("Invalid value for ModuleName. Must be ALL | LITE | REG");
            }
            if (!(operation == "all" || operation == "read" || operation == "write" || operation == "report" || operation == "unlock"))
            {
                throw new Exception("Invalid value for op. Must be ALL | READ | WRITE | REPORT | UNLOCK");
            }
            //
            //having second thoughts for "runfor"...may be not needed
            //
            //to do validate the couriers??

        }

        private static void ReadSystemConfig(out string pgSchema, out string pgConnection, out string logFileName)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            logFileName = configuration["logFileName"];
            if (string.IsNullOrEmpty(logFileName))
                throw new Exception("logFileNm Not in appSettings.json. ABORTING");

            pgSchema = configuration["pgSchema"];
            pgConnection = configuration["pgConnection"];

            if (string.IsNullOrEmpty(pgSchema))
            {
                Logger.Write("ProgramNpsApy", "ReadSystemConfig", 0, "pgSchema Not in appSettings.json. ABORTING", Logger.ERROR);
                throw new Exception("pgSchema Not in appSettings.json. ABORTING");
            }

            if (string.IsNullOrEmpty(pgConnection))
            {
                Logger.Write("ProgramNpsApy", "ReadSystemConfig", 0, "pgConnection Not in appSettings.json. ABORTING", Logger.ERROR);
                throw new Exception("pgConnection Not in appSettings.json. ABORTING");
            }

            if (DbUtil.CanConnectToDB(pgConnection) == false)
                throw new Exception("pgConnection Not in appSettings.json. ABORTING");
        }
        #region TEST_CODE

        private static void TestWrFile()
        {
            string hex = "";
            string txtFile = @"C:\Zunk\Lite\work\20210620\nps_lite\NPSLite\someImage.txt";
            using (StreamReader sr= new StreamReader(txtFile))
            {
                hex = sr.ReadToEnd();
            }
            var bytes = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();

            File.WriteAllBytes(@"C:\Zunk\Lite\work\20210620\nps_lite\NPSLite\somImage.jpg", bytes);
        }

        public static byte[] StringToByteArray(string hex)
        {
            hex = hex.Substring(2, hex.Length - 2);
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hex));
            }

            byte[] HexAsBytes = new byte[hex.Length / 2];

            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hex.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        private static void TestInp()
        {
            Logger.SetLogFileName(@"C:\\Zunk\\POC_Log.txt");

            string pgConnection = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simpleuser; ";
            string pgSchema = "ventura";
            string moduleName = "LiteTest";
            string inputFilePathName = @"C:\d\Personal\Learning\reg_test_in\PTGPRN0507202114070521001.TXT";
            string jsonParamFilePath = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\InputDefine.json";
            int jobId = 0;
            char theDelim = '^';
            Dictionary<string, string> paramsDict = new Dictionary<string, string>();
            paramsDict.Add("systemdir", "c:/users/spadte/source/repos/padtes/DotNetCorePOC/ddl_sql");
            paramsDict.Add("inputdir", "c:/zunk/lite/input");
            paramsDict.Add("workdir", "c:/zunk/lite/work");
            paramsDict.Add("output_par", "nps_lite");
            paramsDict.Add("output_lite", "NPSLite");
            paramsDict.Add("output_apy", "APY");
            //paramsDict.Add("output_duplicate", "nps_lite_copy");
            paramsDict.Add("photo_max_per_dir", "150");
            
            string dateAsDir = "20210620";

            bool suc = FileProcessorUtil.SaveInputToDB(pgConnection, pgSchema, moduleName, jobId, inputFilePathName, jsonParamFilePath, theDelim, paramsDict, dateAsDir);
            if (suc)
                Console.WriteLine("Great Success");
        }
        private static void TestJasonLoad()
        {
            Logger.SetLogFileName(@"C:\\Zunk\\POC_Log.txt");

            string jsonParamFilePath = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\InputDefine.json";

            Dictionary<string, List<string>> fileDefDict = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> jsonSkip = new Dictionary<string, List<string>>();
            Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new Dictionary<string, List<KeyValuePair<string, string>>>();
            SaveAsFileDef saveAsFileDefnn = new SaveAsFileDef();
            SystemParamInput inpSysParam = new SystemParamInput();

            FileProcessorUtil.LoadJsonParamFile(jsonParamFilePath, dbMap, jsonSkip, fileDefDict, saveAsFileDefnn, inpSysParam);

            Console.WriteLine("Great Success");
        }
        #endregion

    }
}
