using CommonUtil;
using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataProcessor
{
    public abstract class FileProcessor
    {
        private const string logProgName = "FileProcessor";

        protected string pgConnection;
        protected string pgSchema;
        protected string operation;
        public int jobId { get; set; }

        protected string systemConfigDir;
        protected string inputRootDir;
        protected string workDir;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();
        protected List<string> staticParamList = new List<string>();

        public FileProcessor(string connectionStr, string schemaName, string operationName)
        {
            pgConnection = connectionStr;
            pgSchema = schemaName;
            operation = operationName;
        }

        public string GetSchema() { return pgSchema; }
        public string GetConnection() { return pgConnection; }

        public static FileProcessor GetProcessorInstance(string moduleName, string connectionStr, string schemaName, string operation)
        {
            FileProcessor fp = null;
            if (moduleName == ConstantBag.MODULE_LITE)
                fp = new FileProcessorLite(connectionStr, schemaName, operation);
            else
                fp = new FileProcessorRegular(connectionStr, schemaName, operation);

            return fp; 
        }

        public virtual bool ProcessModule(string operation, string runFor, string courierCsv, string fileType)
        {
            Logger.WriteInfo(logProgName, "ProcessModule", 0
                , $"START {GetModuleName()} op:{operation} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}, {(courierCsv == "" ? "" : " courier:" + courierCsv)}");

            //timer.start

            try
            {
                if (operation == "all" || operation == "read")
                {
                    LoadModuleParam(runFor, courierCsv);
                    // process input
                    ProcessInput(runFor);
                    //unlock all courier serial number records 
                    SequenceGen.UnlockAll(pgConnection, pgSchema);
                }

                if (operation == "all" || operation == "write")
                {
                    //process output
                    var rep = GetReportProcessor(operation);
                    rep.ProcessOutput(runFor, courierCsv, fileType);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgName, "ProcessNpsLiteApy", 0, ex);
                return false;
            }

            Logger.WriteInfo(logProgName, "ProcessBiz", 0
                , $"GOOD Job!! {GetModuleName()} op:{operation} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}, {(courierCsv == "" ? "" : " courier:" + courierCsv)}");

            return true;
        }

        protected abstract void LoadModuleParam(string runFor, string courierCsv);
        public abstract string GetModuleName();
        public abstract void ProcessInput(string runFor);

        public abstract ReportProcessor GetReportProcessor(string operation);

        public abstract string GetBizTypeImageDirName(InputRecordAbs inputRecord);

        #region JUNK
        //--input file definition json
        //--letter template, letter tags mapping json
        //--other output file def jsons

        #endregion

    }
}