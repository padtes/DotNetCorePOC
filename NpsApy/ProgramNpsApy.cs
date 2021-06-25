using DbOps;
using Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NpsApy
{
    class ProgramNpsApy
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && args[0] == "-help"))
            {
                Console.WriteLine("enter command as ProgramNpsApy -bizType=[Lite|Reg|All] -op=[Read|Write|Report] -runFor=[All|directory name yyyymmdd] -courier=[OPTIONAL courier code(s) as csv For WRITE]");
                Console.WriteLine("for ex. NOTE the DASH");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=Read");
                Console.WriteLine("ProgramNpsApy -bizType=Lite -op=write -courier=ABC,PQR");
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
                FileProcessor processor = FileProcessor.GetProcessorInstance(FileProcessor.BIZ_LITE, pgSchema, pgConnection);

                run = processor.ProcessBiz(operation, runFor, courierCcsv);
            }

            if (bizType == "reg" || bizType == "all") //NPS Lite + APY
            {
                FileProcessor processor = FileProcessor.GetProcessorInstance(FileProcessor.BIZ_REG, pgSchema, pgConnection);

                run = processor.ProcessBiz(operation, runFor, courierCcsv);
            }
            return run;
        }

        private static void ParseCommandArgs(string[] args, out string bizType, out string operation, out string runFor, out string courierCSV)
        {
            //enter command as ProgramNpsApy -bizType=Lite -op=Read -runFor=ALL -courier=ABC,PQR 

            bizType = "all";
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

            if (cmdArgs.ContainsKey("biztype"))
            {
                bizType = cmdArgs["biztype"]; //ALL | LITE | REG 
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

            if (!(bizType == "all" || bizType == "lite" || bizType == "reg"))
            {
                throw new Exception("Invalid value for bizType. Must be ALL | LITE | REG");
            }
            if (!(operation == "all" || operation == "read" || operation == "write" || operation == "report"))
            {
                throw new Exception("Invalid value for op. Must be ALL | READ | WRITE | REPORT");
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
    }
}
