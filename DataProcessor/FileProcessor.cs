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

        protected string pgSchema;
        protected string pgConnection;
        public int jobId { get; set; }

        protected string systemConfigDir;
        protected string inputRootDir;
        protected string workDir;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();
        protected List<string> staticParamList = new List<string>();

        public FileProcessor(string schemaName, string connectionStr)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;
        }

        protected void LoadParam(string bizType)
        {
            //read details based on date from system param table
            string sysParamStr = DbUtil.GetParamsJsonStr(pgConnection, pgSchema, GetModuleName(), bizType, jobId, logProgName);
            if (sysParamStr == "")
            {
                Logger.Write(logProgName, "LoadParam.1", 0, GetModuleName() + "_" + bizType + " record not in system_param table", Logger.ERROR);
                throw new Exception(GetModuleName() + "_" + bizType + " record not in system_param table");
            }
            try
            {
                paramsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sysParamStr);
                systemConfigDir = paramsDict["systemdir"];
                inputRootDir = paramsDict["inputdir"];
                workDir = paramsDict["workdir"];

                if (systemConfigDir == "" || inputRootDir == "" || workDir == "")
                {
                    Logger.Write(logProgName, "LoadParam.2", 0, GetModuleName() + "_" + bizType + " directory param blank", Logger.ERROR);
                    throw new Exception(GetModuleName() + "_" + bizType + " directory param blank");
                }
                systemConfigDir.TrimEnd('/');
                inputRootDir.TrimEnd('/');
                workDir.TrimEnd('/');
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgName, "LoadParam.2", 0, ex);
                throw new Exception(GetModuleName() + "_" + bizType + " directory param in error", ex);  //key not found
            }

            //confirm all the dirs exist  // if not exist, create and re-confirm OR die
            ConfirmDirExists(dirName: systemConfigDir, createIfMissing: false);
            ConfirmDirExists(dirName: inputRootDir, createIfMissing: false);
            ConfirmDirExists(dirName: workDir, createIfMissing: true);
        }

        private void ConfirmDirExists(string dirName, bool createIfMissing)
        {
            if (Directory.Exists(dirName))
                return;

            if (createIfMissing)
                Directory.CreateDirectory(dirName);

            if (Directory.Exists(dirName) == false)
            {
                Logger.Write(logProgName, "ConfirmDirExists", 0, dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""), Logger.ERROR);
                throw new Exception(dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""));
            }
        }

        public static FileProcessor GetProcessorInstance(string moduleName, string schemaName, string connectionStr)
        {
            FileProcessor fp = null;
            if (moduleName == ConstantBag.MODULE_LITE)
                fp = new FileProcessorLite(schemaName, connectionStr);
            else
                fp = new FileProcessorRegular(schemaName, connectionStr);

            return fp; 
        }

        public virtual bool ProcessModule(string operation, string runFor, string courierCsv)
        {
            Logger.WriteInfo(logProgName, "ProcessBiz", 0
                , $"START {GetModuleName()} op:{operation} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}, {(courierCsv == "" ? "" : " courier:" + courierCsv)}");

            //timer.start

            try
            {
                if (operation == "all" || operation == "read")
                {
                    // process input
                    ProcessInput(runFor);
                }

                if (operation == "all" || operation == "write")
                {
                    //process output
                    ProcessOutput(runFor, courierCsv);
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
        public abstract string GetModuleName();
        public abstract void ProcessInput(string runFor);
        public abstract void ProcessOutput(string runFor, string courierCcsv);

        protected void ValidateStaticParam(string bizType)
        {
            String erMsg = "";

            foreach (string pName in staticParamList)
            {
                if (!(paramsDict.ContainsKey(pName) && paramsDict[pName] != ""))
                    erMsg += pName + " ";
            }

            if (erMsg != "")
            {
                Logger.Write(logProgName, "ValidateStaticParam", 0, erMsg + " params are missing for " + GetModuleName() + "_" + bizType, Logger.ERROR);
                throw new Exception(erMsg + " params are missing for " + GetModuleName() + "_" + bizType);
            }
        }

        #region JUNK
        //--input file definition json
        //--letter template, letter tags mapping json
        //--other output file def jsons

        #endregion

    }
}