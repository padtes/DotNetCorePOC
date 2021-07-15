using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class ReportProcessorLite : ReportProcessor
    {
        public ReportProcessorLite(string connectionStr, string schemaName, string module, string opName) : base(connectionStr, schemaName, module, opName)
        {
        }

        public override string GetProgName()
        {
            return "ReportProcessorLite";
        }
        public override string GetBizType()
        {
            return ConstantBag.LITE_OUT_RESPONSE;
        }
        public override void WriteOutput(string runFor, string courierCsv, string fileType)
        {
            if (fileType == "resp") //immdeiate resp //to do define const
            {
                WriteImmResponse(runFor, courierCsv);
            }
            else if (fileType == "stat") //status report //to do define const
            {
                WriteStatusReport(runFor, courierCsv);
            }
            else
            {
                ProcessNpsLiteOutput(runFor, courierCsv);

                ProcessApyOutput(runFor, courierCsv);
            }
        }

        private FileTypeMaster GetFTypeMaster(string bizType)
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
        private void WriteImmResponse(string runFor, string courierCsv)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                --- workDir / ddmmyyyy / nps_lite_apy / nps 
                --- workDir / ddmmyyyy / nps_lite_apy / nps / <response file>
             */

            string bizType = ConstantBag.LITE_OUT_RESPONSE;
            FileTypeMaster fTypeMaster = GetFTypeMaster(bizType);
            if (fTypeMaster == null)
                return;

            string bizTypeToRead = ConstantBag.LITE_IN;
            string bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];
            ProcessNpsApyLiteOutput(bizTypeToRead, bizType, fTypeMaster, runFor, bizDir, false);

            bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_APY_DIR];
            ProcessNpsApyLiteOutput(bizTypeToRead, bizType, fTypeMaster, runFor, bizDir, true);

        }

        private void ProcessNpsApyLiteOutput(string bizTypeToRead, string bizTypeToWrite, FileTypeMaster fTypeMaster, string workdirYmd, string bizDir, bool isApy)
        {
            string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
                        + "\\" + workdirYmd// "yyyymmdd" 
                        + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
                        + "\\" + bizDir // module + bizType based dir = ("output_apy or output_lite") 
                        ;

            string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            CsvReportUtil csvRep = new CsvReportUtil(GetConnection(), GetSchema(), moduleName, bizTypeToRead, JobId, jsonCsvDef, outputDir);

            string fileName = fTypeMaster.fnamePattern
                .Replace("{{sys_param(printer_code)}}", paramsDict[ConstantBag.PARAM_PRINTER_CODE3])
                .Replace("{{now_ddmmyy}}", DateTime.Now.ToString("ddMMyy")); //TO DO : parse the file name pattern

            //TO DO get serial number - add rec if not found
            string tmpFileName = fileName.Replace("{{Serial No}}", "");
            string serNo = SequenceGen.GetNextSequence(GetConnection(), GetSchema(), "generic", tmpFileName, 2, addIfNeeded: true, unlock: true);  //to do define const for generic
            fileName = fileName.Replace("{{Serial No}}", serNo);

            string[] args = { }; //DateTime.Now.ToString("dd-MMM-yyyy")  
 
            RootJsonParamCSV csvConfig = csvRep.GetCsvConfig();
            string wherePart = "lower(apy_flag) = '" + (isApy ? "y" : "n") + "'";

            DataSet ds = GetReportDS(pgConnection, pgSchema, moduleName, bizTypeToRead, bizTypeToWrite, JobId
            , csvConfig, args, workdirYmd, wherePart);

            if (ds == null)
            {
                return;
            }

            string doneAction = "";
            if (bizTypeToWrite == ConstantBag.LITE_OUT_RESPONSE)
            {
                doneAction = ConstantBag.DET_LC_STEP_RESPONSE1;
            }
            else if (bizTypeToWrite == ConstantBag.LITE_OUT_STATUS)
            {
                doneAction = ConstantBag.DET_LC_STEP_STAT_REP3;
            }

            csvRep.CreateFile(workdirYmd, fileName, args, paramsDict, ds, doneAction);
        }

        private void WriteStatusReport(string runFor, string courierCsv)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                --- workDir / ddmmyyyy / nps_lite_apy / nps 
                --- workDir / ddmmyyyy / nps_lite_apy / nps / <status file> 
             */

            string bizType = ConstantBag.LITE_OUT_STATUS;
            FileTypeMaster fTypeMaster = GetFTypeMaster(bizType);
            if (fTypeMaster == null)
                return;

            string bizTypeToRead = ConstantBag.LITE_IN;
            string bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];
            ProcessNpsApyLiteOutput(bizTypeToRead, bizType, fTypeMaster, runFor, bizDir, false);

            bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_APY_DIR];
            ProcessNpsApyLiteOutput(bizTypeToRead, bizType, fTypeMaster, runFor, bizDir, true);
        }

        private void ProcessNpsLiteOutput(string runFor, string courierCsv)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                workDir / ddmmyyyy / nps_lite_apy / nps / 
                workDir / ddmmyyyy / nps_lite_apy / nps / courier_name_ddmmyy / <PTC file> <card file> <letter files> 
             */
            //collect what all couriers to process
            //for each courier
            //create outputs
            throw new NotImplementedException();
        }

        private void ProcessApyOutput(string runFor, string courierCsv)
        {
            /*
            OUTPUT file structure 
            --APY
                workDir / ddmmyyyy / nps_lite_apy / apy
                workDir / ddmmyyyy / nps_lite_apy / apy / courier_name_ddmmyy / <PTC file> <card file> <letter files>
             */
            throw new NotImplementedException();
        }

        protected override void LoadModuleParam(string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { ConstantBag.PARAM_OUTPUT_PARENT_DIR, ConstantBag.PARAM_OUTPUT_LITE_DIR, ConstantBag.PARAM_OUTPUT_APY_DIR
            , ConstantBag.PARAM_PRINTER_CODE2, ConstantBag.PARAM_PRINTER_CODE3};

            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, GetProgName(), moduleName, JobId
                , out systemConfigDir, out inputRootDir, out workDir);

            ProcessorUtil.ValidateStaticParam(moduleName, GetBizType(), GetProgName(), paramsDict, staticParamList);
        }

        public override DataSet GetReportDS(string pgConnection, string pgSchema, string moduleName, string bizTypeToRead, string bizTypeToWrite, int jobId
            , RootJsonParamCSV csvConfig, string[] progParams, string workdirYmd, string wherePart)
        {
            string waitingAction, doneAction;
            SetupActions(bizTypeToWrite, out waitingAction, out doneAction);
            StringBuilder colSelectionSb = new StringBuilder();
            SqlHelper.GetSelectColumns(csvConfig.Detail, csvConfig.System, progParams, paramsDict, colSelectionSb);

            DataSet ds = DbUtil.GetFileDetailList(pgConnection, pgSchema, GetProgName(), moduleName, bizTypeToRead, jobId
            , colSelectionSb.ToString(), waitingAction, doneAction, workdirYmd, wherePart, csvConfig.System.DataOrderby, out string sql);

            if (ds == null || ds.Tables.Count < 1)
            {
                Logger.Write(GetProgName(), "GetReportDS", 0, "No Table returned check sql", Logger.WARNING);
                Logger.Write(GetProgName(), "GetReportDS", 0, "sql:" + sql, Logger.WARNING);

                return null;
            }

            return ds;
        }

        private static void SetupActions(string bizTypeToWrite, out string waitingAction, out string doneAction)
        {
            if (bizTypeToWrite == ConstantBag.LITE_OUT_RESPONSE)
            {
                waitingAction = ConstantBag.DET_LC_STEP_RESPONSE1;
                doneAction = "";
            }
            else if (bizTypeToWrite == ConstantBag.LITE_OUT_STATUS)
            {
                waitingAction = ConstantBag.DET_LC_STEP_STAT_REP3;
                doneAction = ConstantBag.DET_LC_STEP_STAT_UPD2;
            }
            else
            {
                throw new Exception("GetReportDS: Not handled bizTypeToWrite " + bizTypeToWrite);
            }
        }
    }

}
