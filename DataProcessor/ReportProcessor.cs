﻿using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public abstract class ReportProcessor
    {
        protected string pgConnection;
        protected string pgSchema;
        protected string moduleName;
        protected string operation;
        protected string fileType;
        public int JobId { get; set; }

        protected string systemConfigDir;
        protected string inputRootDir;
        protected string workDir;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();
        protected List<string> staticParamList = new List<string>();

        public ReportProcessor(string connectionStr, string schemaName, string module, string opName, string fileTypeNm)
        {
            pgConnection = connectionStr;
            pgSchema = schemaName;
            moduleName = module;
            operation = opName;
            fileType = fileTypeNm;
        }
        public abstract string GetProgName();
        public abstract string GetBizType();
        protected abstract void LoadModuleParam(string runFor, string courierCsv);
        public abstract void WriteOutput(string runFor, string courierCsv);

        public abstract DataSet GetReportDS(string pgConnection, string pgSchema, string moduleName, string bizTypeToRead, string bizTypeToWrite, int jobId
            , RootJsonParamCSV csvConfig, string[] progParams, string workdirYmd, string wherePart);

        public virtual void ProcessOutput(string runFor, string courierCsv)
        {
            Logger.WriteInfo(GetProgName(), "ProcessOutput", 0
                , $"START {moduleName} op:{operation} fileType:{fileType} parameters: {runFor} {(courierCsv == "" ? "" : " courier:" + courierCsv)} ");

            LoadModuleParam(runFor, courierCsv);
            WriteOutput(runFor, courierCsv);
        }

        public string GetSchema() { return pgSchema; }
        public string GetConnection() { return pgConnection; }

        public FileTypeMaster GetFTypeMaster(string bizType)
        {
            FileTypeMaster fTypeMaster = DbUtil.GetFileTypeMaster(pgConnection, pgSchema, moduleName, bizType, JobId);

            if (fTypeMaster == null)
            {
                Logger.Write(GetProgName(), "ProcessNpsApyLiteOutputImmResp", 0
                    , $"NO FileTypeMaster for {bizType} module:{moduleName} parameters: system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}"
                    , Logger.ERROR);

                return null; //----------------------------
            }

            string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            if (File.Exists(jsonCsvDef) == false)
            {
                Logger.Write(GetProgName(), "ProcessNpsApyLiteOutputImmResp", 0
                    , $"FILE NOT FOUND: {jsonCsvDef}. Aborting. Check Filemaster  {bizType} for file name"
                    , Logger.ERROR);
                return null; //----------------------------
            }

            return fTypeMaster;
        }

    }

}
