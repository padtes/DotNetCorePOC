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
using PanProcessor;

namespace NpsApy
{
    class ProgramNpsApy
    {
        private static string prg_version = "v2.01.0 Change Date:2021-Apr-23. PAN";
        static void Main(string[] args)
        {
            //TestSomething();

            //to do validate file based on column def - need to change col def for length, value range, empty ot not

            if (args.Length == 0 || (args.Length == 1 && args[0] == "-help"))
            {
                Console.WriteLine("version :" + prg_version);
                Console.WriteLine("enter command as following:");
                Console.WriteLine("ProgramNpsApy -modulename=[Lite|Reg|All|PAN] -op=[Read|Write|Report|updstat] -file=[resp|status|..] -runFor=[All|directory name yyyymmdd] -courier=[OPTIONAL courier code(s) as csv For WRITE]");
                Console.WriteLine("for ex. NOTE the DASH");
                Console.WriteLine("ProgramNpsApy -moduleName=Lite -op=Read -runfor=20210726");
                Console.WriteLine("ProgramNpsApy -moduleName=Lite -op=write -file=letter -runfor=20210726");
                Console.WriteLine("ProgramNpsApy -moduleName=PAN -op=Read -runfor=20211204");
                Console.WriteLine("ProgramNpsApy -v");
                //Console.WriteLine("---- special case ----");
                //Console.WriteLine("ProgramNpsApy -moduleName=Lite -op=UNLOCK  : this will unlock Courier Counter lock");
                return;
            }

            if (args.Length == 1 && (args[0].Equals("-v", StringComparison.InvariantCultureIgnoreCase)
                || args[0].Equals("-ver", StringComparison.InvariantCultureIgnoreCase)
                || args[0].Equals("-version",StringComparison.InvariantCultureIgnoreCase)  ))
            {
                Console.WriteLine("version :" + prg_version);
                return;
            }
            string pgSchema, pgConnection, logFileName, deleteDir;

            // read appSettings.json config
            ReadSystemConfig(out pgSchema, out pgConnection, out logFileName, out deleteDir);
            Logger.SetLogFileName(logFileName);

            string paramsMsg = string.Join(' ', args);

            //get runtime parameter
            string modType;    //valid values ALL | LITE | REG | PAN
            string operation; //Op == Operation: READ or WRITE or REPORT -   All == READ as well as WRITE
            string runFor;    //default - no param - scan base directory and process all
            string fileType;    //default - no param - write file : RESP = Response or STATUS or ...
            string courierCSV;

            ParseCommandArgs(args, out modType, out operation, out runFor, out courierCSV, out fileType, out string fileSubTpe);

            Logger.Write("ProgramNpsApy", "main", 0, "==================== ================= ", Logger.INFO);
            Logger.Write("ProgramNpsApy", "main", 0, "==================== Nps APY version :" + prg_version + " Run Start " + string.Join(' ', args), Logger.INFO);
            Logger.Write("ProgramNpsApy", "main", 0, "==================== ================= ", Logger.INFO);

            bool runResult;
            if (modType == "pan")
                runResult = Mediator.ProcessPAN(pgConnection, pgSchema, operation, modType, runFor, courierCSV, fileType, fileSubTpe, deleteDir);
            else
                runResult = ProcessNpsApy(pgConnection, pgSchema, operation, modType, runFor, courierCSV, fileType, deleteDir);

            if (runResult == false)
            {
                Logger.Write("ProgramNpsApy", "main", 0, "** ** ** ** Run Failed ** ** ** ** " + paramsMsg, Logger.ERROR);
                //Console.WriteLine("Run Failed " + paramsMsg);
            }
            else
            {
                Logger.Write("ProgramNpsApy", "main", 0, "!! !! Run Success !! " + paramsMsg, Logger.INFO);
                //Console.WriteLine("Run Success " + paramsMsg);
            }

        }
 
        private static bool ProcessNpsApy(string pgConnection, string pgSchema, string operation, string modType, string runFor, string courierCSV, string fileType, string deleteDir)
        {
            bool runResult;
            if (operation == "unlock")
            {
                bool ok = DbUtil.Unlock(pgConnection, pgSchema);
                if (ok)
                    Console.WriteLine("Unlock done");
                else
                    Console.WriteLine("Unlock FAILED, check exceptions");

                return true;
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

                SimpleReport rep = new SimpleReport(pgSchema, pgConnection);
                runResult = rep.Print(modType, runFor, fileType);
            }
            else
            {
                DbUtil.Unlock(pgConnection, pgSchema);
                runResult = ProcessData(pgSchema, pgConnection, modType, operation, runFor, courierCSV, fileType, deleteDir);
            }
            return runResult;
        }

        private static bool ProcessData(string pgSchema, string pgConnection, string modType, string operation, string runFor, string courierCcsv, string fileType, string deleteDir)
        {
            bool run = false;

            if (modType == "lite" || modType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_LITE, pgConnection, pgSchema, operation, fileType);

                run = processor.ProcessModule(operation, runFor, courierCcsv, fileType, ConstantBag.ALL, deleteDir);
            }

            if (modType == "reg" || modType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(ConstantBag.MODULE_REG, pgConnection, pgSchema, operation, fileType);

                run = processor.ProcessModule(operation, runFor, courierCcsv, fileType, ConstantBag.ALL, deleteDir);
            }
            return run;
        }

        private static void ParseCommandArgs(string[] args, out string moduleName, out string operation, out string runFor, out string courierCSV
            , out string fileType, out string fileSubType)
        {
            //enter command as ProgramNpsApy -bizType=Lite -op=Read -runFor=ALL -courier=ABC,PQR 

            moduleName = "all";
            operation = "all";
            runFor = "all";
            courierCSV = "";
            fileType = "";
            fileSubType = string.Empty;

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
                    //Console.WriteLine(m.Groups[1].Value + "::" + m.Groups[2].Value);
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
            if (cmdArgs.ContainsKey("subfile"))
            {
                fileSubType = cmdArgs["subfile"];
            }

            if (!(moduleName == "all" || moduleName == "lite" || moduleName == "reg" || moduleName == "pan"))
            {
                throw new Exception("Invalid value for ModuleName. Must be ALL | LITE | REG | PAN");
            }
            if (!(operation == "all" || operation == "read" || operation == "write" || operation == "report" 
                || operation == "unlock" || operation == "updstat"))
            {
                throw new Exception("Invalid value for op. Must be ALL | READ | WRITE | REPORT | UNLOCK | UPDSTAT");
            }
            if (moduleName == "pan" && operation == "write" && string.IsNullOrEmpty(fileSubType))
                    throw new Exception("subFile value missing for PAN write");

            if (string.IsNullOrEmpty(fileSubType) || fileSubType.ToLower() == ConstantBag.ALL)
                fileSubType = ConstantBag.ALL;
            if (fileSubType.ToLower() != ConstantBag.ALL)
                fileSubType = fileSubType.ToUpper();

            //
            //having second thoughts for "runfor"...may be not needed
            //
            //to do validate the couriers??

        }

        private static void ReadSystemConfig(out string pgSchema, out string pgConnection, out string logFileName, out string deleteDir)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            logFileName = configuration["logFileName"];
            if (string.IsNullOrEmpty(logFileName))
                throw new Exception("logFileNm Not in appSettings.json. ABORTING");
            Logger.SetLogFileName(logFileName);

            pgSchema = configuration["pgSchema"];
            pgConnection = configuration["pgConnection"];
            deleteDir = configuration["dirForTrash"];

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
