using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace NpsApy
{
    internal abstract class FileProcessor
    {
        private const string moduleName = "FileProcessor";
        public const string BIZ_LITE = "lite";
        public const string BIZ_REG = "reg";

        protected string pgSchema;
        protected string pgConnection;

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
            string sysParamStr = DbUtil.GetParamsJsonStr(pgConnection, pgSchema, bizType, "directories");
            if (sysParamStr == "")
            {
                Logger.Write(moduleName, "LoadParam.1", 0, bizType + " directory struct not in system_param table", Logger.ERROR);
                throw new Exception(bizType + " directory struct not in system_param table");
            }
            try
            {
                paramsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sysParamStr);
                systemConfigDir = paramsDict["systemdir"];
                inputRootDir = paramsDict["inputdir"];
                workDir = paramsDict["workdir"];

                if (systemConfigDir == "" || inputRootDir == "" || workDir == "")
                {
                    Logger.Write(moduleName, "LoadParam.2", 0, bizType + " directory param blank", Logger.ERROR);
                    throw new Exception(bizType + " directory param blank");
                }
                systemConfigDir.TrimEnd('/');
                inputRootDir.TrimEnd('/');
                workDir.TrimEnd('/');
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "LoadParam.2", 0, ex);
                throw new Exception(bizType + " directory param in error", ex);  //key not found
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
                Logger.Write(moduleName, "ConfirmDirExists", 0, dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""), Logger.ERROR);
                throw new Exception(dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""));
            }
        }

        internal static FileProcessor GetProcessorInstance(string bizType, string schemaName, string connectionStr)
        {
            FileProcessor fp = null;
            if (bizType == BIZ_LITE)
                fp = new FileProcessorLite(schemaName, connectionStr);
            else
                fp = new FileProcessorRegular(schemaName, connectionStr);

            return fp; 
        }

        public virtual bool ProcessBiz(string operation, string runFor, string courierCsv)
        {
            Logger.WriteInfo(moduleName, "ProcessNpsLiteApy", 0
                , $"LITE op:{operation} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}, {(courierCsv == "" ? "" : " courier:" + courierCsv)}");

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
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "ProcessNpsLiteApy", 0, ex);
                return false;
            }

        }
        public abstract string GetBizType();
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
                Logger.Write(moduleName, "ValidateStaticParam", 0, erMsg + " params are missing for " + bizType, Logger.ERROR);
                throw new Exception(erMsg + " params are missing for " + bizType);
            }
        }

        #region JUNK
        //--input file definition json
        //--letter template, letter tags mapping json
        //--other output file def jsons

        #endregion

    }
}