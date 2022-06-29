using CommonUtil;
using DataProcessor;
using DbOps;
using DbOps.Structs;
using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace PanProcessor
{
    class ReportProcessorPan : ReportProcessor
    {
        public ReportProcessorPan(string connectionStr, string schemaName, string module, string opName, string fileType)
            : base(connectionStr, schemaName, module, opName, fileType)
        {
        }

        public override string GetBizType()
        {
            if (fileType == ConstantBag.PAN_OUT_CARD_INDV || fileType == ConstantBag.PAN_OUT_CARD_CORP || fileType == ConstantBag.PAN_OUT_CARD_EKYC)
                return fileType;

            Logger.Write(GetProgName(), "", 0, "Invalid file type " + fileType, Logger.ERROR);

            throw new DataException("Invalid file type for report processor " + fileType ?? "");
        }
        public string GetOutputDirParamNameBizType()
        {
            if (fileType == ConstantBag.PAN_OUT_CARD_INDV)
                return ConstantBag.PARAM_OUTPUT_DIR_PAN_INDV;

            if (fileType == ConstantBag.PAN_OUT_CARD_CORP)
                return ConstantBag.PARAM_OUTPUT_DIR_PAN_CORP;

            if (fileType == ConstantBag.PAN_OUT_CARD_EKYC)
                return ConstantBag.PARAM_OUTPUT_DIR_PAN_EKYC;

            Logger.Write(GetProgName(), "", 0, "Invalid file type for param name" + fileType, Logger.ERROR);

            throw new DataException("Invalid file type for report processor param name" + fileType ?? "");
        }

        public override string GetProgName()
        {
            return "ReportProcessorPan";
        }

        public override DataSet GetReportDS(string pgConnection, string pgSchema, string moduleName, string bizTypeToRead, string bizTypeToWrite, int jobId, RootJsonParamCSV csvConfig, string[] progParams, string workdirYmd, string wherePart)
        {
            string waitingAction, doneAction;
            SetupActions(bizTypeToWrite, out waitingAction, out doneAction);
            return GetReportDSByActions(pgConnection, pgSchema, moduleName, bizTypeToRead, jobId, csvConfig, progParams, workdirYmd, wherePart, waitingAction, doneAction);
        }

        public override void WriteOutput(string runFor, string courierCsv)
        {
            /*
            OUTPUT file structure - may change
            --PAN
                workDir / ddmmyyyy / pan / indiv / courier_name_ddmmyy / <card file>
                workDir / ddmmyyyy / pan / corp / courier_name_ddmmyy / <card file>
             */
            string bizTypeToRead = ConstantBag.PAN_IN;

            string bizDir = paramsDict[GetOutputDirParamNameBizType()];

            ProcessPanCard(bizTypeToRead, runFor, bizDir, courierCsv);

        }

        private void ProcessPanCard(string bizTypeToRead, string workdirYmd, string bizDir, string courierCsv)
        {
            string bizType = fileType;
            FileTypeMaster fTypeMaster = GetFTypeMaster(bizType, "ProcessPanCard");
            if (fTypeMaster == null)
                return;

            SetupActions(bizType, out string waitingAction, out string doneAction);

            //collect what all couriers to process
            List<string> courierList = new List<string>();

            DbUtil.GetCouriers(GetConnection(), GetSchema(), GetProgName(), moduleName, bizTypeToRead, JobId
                , workdirYmd, waitingAction, doneAction, courierList, courierCsv, "", out string sql);

            Logger.Write(GetProgName(), "ProcessPanCard", 0, "sql:" + sql, Logger.INFO);
            if (courierList.Count < 1)
            {
                Logger.Write(GetProgName(), "ProcessPanCard", 0, $"No records found for Card {workdirYmd} courr: {courierCsv} {bizType}", Logger.WARNING);
                return;
            }

            Logger.WriteInfo(GetProgName(), "ProcessPanCard", 0, $"{String.Join(",", courierList.ToArray())} found for Card {workdirYmd} courr: {courierCsv} {bizType}");

            string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
            + "\\" + workdirYmd// "yyyymmdd" 
            + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
            + "\\" + bizDir // module + bizType based dir = ("output_apy or output_lite") 
            ;

            string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            CsvReportUtil csvRep = new CsvReportUtil(GetConnection(), GetSchema(), moduleName, bizTypeToRead, JobId, jsonCsvDef, outputDir);

            foreach (string courierId in courierList)
            {
                ProcessPanCardCourier(csvRep, bizTypeToRead, bizType, fTypeMaster, workdirYmd, bizDir, courierId);
            }
        }

        private void ProcessPanCardCourier(CsvReportUtil csvRep, string bizTypeToRead, string bizTypeToWrite, FileTypeMaster fTypeMaster, string workdirYmd, string bizDir, string courierId)
        {
            string fileName = fTypeMaster.fnamePattern
                .Replace("{{sys_param(printer_code)}}", paramsDict[ConstantBag.PARAM_PRINTER_CODE3])
                .Replace(ConstantBag.FILE_NAME_TAG_YYMMDD, DateTime.Now.ToString("yyyyMMdd"))
                .Replace(ConstantBag.FILE_NAME_TAG_COUR_CD, courierId); //TO DO : parse the file name pattern

            //TO DO get serial number - add rec if not found
            string tmpFileName = fileName.Replace(ConstantBag.FILE_NAME_TAG_SER_NO, "");
            string pattern = "";
            string cardType = ConstantBag.CARD_PAN; 
            string serNo = SequenceGen.GetNextSequence(false, GetConnection(), GetSchema(), ConstantBag.SEQ_GENERIC, tmpFileName, cardType, ref pattern, 2, addIfNeeded: true, unlock: true);
            fileName = fileName.Replace(ConstantBag.FILE_NAME_TAG_SER_NO, serNo);

            string[] args = { }; //DateTime.Now.ToString("dd-MMM-yyyy")  

            RootJsonParamCSV csvConfig = csvRep.GetCsvConfig();
            string wherePart = $" and courier_id='{courierId}'";

            DataSet ds = GetReportDS(pgConnection, pgSchema, moduleName, bizTypeToRead, bizTypeToWrite, JobId
            , csvConfig, args, workdirYmd, wherePart);

            if (ds == null)
            {
                return;
            }

            string doneAction = ConstantBag.PAN_STEP_CARD_OUT;
            string subDir = Path.Combine($"{courierId}_{workdirYmd}", "Card_Print");

            csvRep.CreateFile(workdirYmd, fileName, subDir, args, paramsDict, ds, doneAction);

        }

        private void SetupActions(string bizTypeToWrite, out string waitingAction, out string doneAction)
        {
            if (bizTypeToWrite == ConstantBag.PAN_OUT_CARD_CORP || bizTypeToWrite == ConstantBag.PAN_OUT_CARD_EKYC || bizTypeToWrite == ConstantBag.PAN_OUT_CARD_INDV)
            {
                waitingAction = ConstantBag.PAN_STEP_CARD_OUT;
                doneAction = ""; //no restriction
            }
            else
            {
                throw new Exception("SetupActions: Not handled bizTypeToWrite " + bizTypeToWrite);
            }
        }

        protected override void LoadModuleParam(string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { ConstantBag.PARAM_OUTPUT_PARENT_DIR
                , ConstantBag.PARAM_OUTPUT_DIR_PAN_CORP, ConstantBag.PARAM_OUTPUT_DIR_PAN_EKYC, ConstantBag.PARAM_OUTPUT_DIR_PAN_INDV
                , ConstantBag.PARAM_PRINTER_CODE2, ConstantBag.PARAM_PRINTER_CODE3};

            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, GetProgName(), moduleName, JobId
                , out systemConfigDir, out inputRootDir, out workDir);

            ProcessorUtil.ValidateStaticParam(moduleName, GetBizType(), GetProgName(), paramsDict, staticParamList);
        }
    }
}
