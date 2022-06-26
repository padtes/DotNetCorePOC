using CommonUtil;
using DataProcessor;
using DbOps.Structs;
using Logging;
using System.Collections.Generic;
using System.Data;

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

            Logger.Write(GetProgName(),"", 0, "Invalid file type " + fileType, Logger.ERROR);

            throw new DataException("Invalid file type for report processor " + fileType??"");
        }

        public override string GetProgName()
        {
            return "ReportProcessorPan";
        }

        public override DataSet GetReportDS(string pgConnection, string pgSchema, string moduleName, string bizTypeToRead, string bizTypeToWrite, int jobId, RootJsonParamCSV csvConfig, string[] progParams, string workdirYmd, string wherePart)
        {
            throw new System.NotImplementedException();
        }

        public override void WriteOutput(string runFor, string courierCsv)
        {

            throw new System.NotImplementedException();
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
