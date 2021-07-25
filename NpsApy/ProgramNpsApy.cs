using CommonUtil;
using DbOps;
using Logging;
using DataProcessor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;

namespace NpsApy
{
    class ProgramNpsApy
    {
        static void Main(string[] args)
        {
            //TestSomething();

            //to do validate file based on column def - need to change col def for length, value range, empty ot not

            if (args.Length == 0 || (args.Length == 1 && args[0] == "-help"))
            {
                Console.WriteLine("enter command as following:");
                Console.WriteLine("ProgramNpsApy -modulename=[Lite|Reg|All] -op=[Read|Write|Report|updstat] -file=[resp|status|..] -runFor=[All|directory name yyyymmdd] -courier=[OPTIONAL courier code(s) as csv For WRITE]");
                Console.WriteLine("for ex. NOTE the DASH");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=Read");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=write -courier=ABC,PQR");
                Console.WriteLine("---- special case ----");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=UNLOCK  : this will unlock Courier Counter lock");
                return;
            }

            string pgSchema, pgConnection, logFileName;

            // read appSettings.json config
            ReadSystemConfig(out pgSchema, out pgConnection, out logFileName);
            Logger.SetLogFileName(logFileName);

            string paramsMsg = string.Join(' ', args);

            //get runtime parameter
            string modType;    //valid values ALL | LITE | REG 
            string operation; //Op == Operation: READ or WRITE or REPORT -   All == READ as well as WRITE
            string runFor;    //default - no param - scan base directory and process all
            string fileType;    //default - no param - write file : RESP = Response or STATUS or ...
            string courierCSV;

            ParseCommandArgs(args, out modType, out operation, out runFor, out courierCSV, out fileType);

            Logger.Write("ProgramNpsApy", "main", 0, "==================== ================= ", Logger.INFO);
            Logger.Write("ProgramNpsApy", "main", 0, "==================== Nps APY Run Start " + string.Join(' ', args), Logger.INFO);
            Logger.Write("ProgramNpsApy", "main", 0, "==================== ================= ", Logger.INFO);

            bool runResult;
            if (operation == "unlock")
            {
                bool ok = DbUtil.Unlock(pgConnection, pgSchema);
                if (ok)
                    Console.WriteLine("Unlock done");
                else
                    Console.WriteLine("Unlock FAILED, check exceptions");

                return;
            }
            if (operation == "updstat" || operation == "super_update")
            {
                //change detail record status to print error or printed or reset sent to print To yet-to-print
                UpdateStatus updateStatus = new UpdateStatus(pgSchema, pgConnection, modType, 0);
                runResult = updateStatus.Update(superUpd: (operation == "super_update"), inputFilePathName: fileType);
            }
            else if (operation == "report")
            {
                //to do summary REPORT will dump simple report of counts by <date>, <LITE | APY | REGULAR>, < COURIER >, count of yet to print cards or in records in error
                //to do courier REPORT will dump < COURIER >, range - from-to and next

                if (fileType != "card")
                {
                    SimpleReport rep = new SimpleReport(pgSchema, pgConnection);
                    runResult = rep.Print(modType, runFor, fileType);
                }
                else
                {
                    CardReportNpsLiteApy rep = new CardReportNpsLiteApy(pgSchema, pgConnection);
                    runResult = rep.PrintAll(modType, 0, runFor, courierCSV);
                }
            }
            else
            {
                if (Debugger.IsAttached)
                    DbUtil.Unlock(pgConnection, pgSchema);
                runResult = ProcessData(pgSchema, pgConnection, modType, operation, runFor, courierCSV, fileType);
            }

            if (runResult == false)
            {
                Logger.Write("ProgramNpsApy", "main", 0, "** ** ** ** Run Failed ** ** ** ** " + paramsMsg, Logger.ERROR);
                Console.WriteLine("Run Failed " + paramsMsg);
            }
            else
            {
                Logger.Write("ProgramNpsApy", "main", 0, "!! !! Run Success !! " + paramsMsg, Logger.INFO);
                Console.WriteLine("Run Success " + paramsMsg);
            }

        }

        private static bool ProcessData(string pgSchema, string pgConnection, string modType, string operation, string runFor, string courierCcsv, string fileType)
        {
            bool run = false;

            if (modType == "lite" || modType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_LITE, pgConnection, pgSchema, operation);

                run = processor.ProcessModule(operation, runFor, courierCcsv, fileType);
            }

            if (modType == "reg" || modType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_REG, pgConnection, pgSchema, operation);

                run = processor.ProcessModule(operation, runFor, courierCcsv, fileType);
            }
            return run;
        }

        private static void ParseCommandArgs(string[] args, out string moduleName, out string operation, out string runFor, out string courierCSV, out string fileType)
        {
            //enter command as ProgramNpsApy -bizType=Lite -op=Read -runFor=ALL -courier=ABC,PQR 

            moduleName = "all";
            operation = "all";
            runFor = "all";
            courierCSV = "";
            fileType = "";

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
            if (cmdArgs.ContainsKey("file"))
            {
                fileType = cmdArgs["file"];
            }

            if (!(moduleName == "all" || moduleName == "lite" || moduleName == "reg"))
            {
                throw new Exception("Invalid value for ModuleName. Must be ALL | LITE | REG");
            }
            if (!(operation == "all" || operation == "read" || operation == "write" || operation == "report" 
                || operation == "unlock" || operation == "updstat"))
            {
                throw new Exception("Invalid value for op. Must be ALL | READ | WRITE | REPORT | UNLOCK | UPDSTAT");
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

        private static void TestSomething()
        {
            Logger.SetLogFileName(@"c:\Zunk\testPOC.txt");

            TestRun.TestCourierSeq();
            //TestRun.TestJasonLoad();

            //TestRun.TestScriban();
        }

    }

}
