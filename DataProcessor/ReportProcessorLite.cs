using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using System;
using System.Collections.Generic;
using System.IO;

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
                string bizType = ConstantBag.LITE_OUT_RESPONSE;
                string bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];

                FileTypeMaster fTypeMaster = DbUtil.GetFileTypeMaster(pgConnection, pgSchema, moduleName, bizType, JobId);

                if (fTypeMaster == null)
                {
                    Logger.Write(GetProgName(), "ProcessNpsApyLiteOutputImmResp", 0
                        , $"NO FileTypeMaster for {bizType} module:{moduleName} parameters: system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}"
                        , Logger.ERROR);

                    return; //----------------------------
                }

                string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
                if (File.Exists(jsonCsvDef) == false)
                {
                    Logger.Write(GetProgName(), "ProcessNpsApyLiteOutputImmResp", 0
                        , $"FILE NOT FOUND: {jsonCsvDef}. Aborting. Check Filemaster  {bizType} for file name"
                        , Logger.ERROR);
                    return; //----------------------------
                }

                string bizTypeToRead = ConstantBag.LITE_IN;
                ProcessNpsApyLiteOutputImmResp(bizTypeToRead, fTypeMaster, runFor, bizDir, false);

                bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_APY_DIR];
                ProcessNpsApyLiteOutputImmResp(bizTypeToRead, fTypeMaster, runFor, bizDir, true);
            }
            else
            {
                ProcessNpsLiteOutput(runFor, courierCsv);

                ProcessApyOutput(runFor, courierCsv);
            }
        }

        private void ProcessNpsApyLiteOutputImmResp(string bizTypeToRead, FileTypeMaster fTypeMaster, string workdirYmd, string bizDir, bool isApy)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                --- workDir / ddmmyyyy / nps_lite_apy / nps 
                --- workDir / ddmmyyyy / nps_lite_apy / nps / <status file> <response file>
             */

            string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
                        + "\\" + workdirYmd// "yyyymmdd" 
                        + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
                        + "\\" + bizDir // module + bizType based dir = ("output_apy or output_lite") 
                        ;

            string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            CsvReportUtil csvRep = new CsvReportUtil(GetConnection(), GetSchema(), moduleName, bizTypeToRead, JobId, jsonCsvDef, outputDir);

            string whPart = "lower(apy_flag) = '" + (isApy ? "y" : "n") +"'";

            string fileName = fTypeMaster.fnamePattern
                .Replace("{{sys_param(printer_code)}}", paramsDict[ConstantBag.PARAM_PRINTER_CODE3])
                .Replace("{{now_ddmmyy}}", DateTime.Now.ToString("ddMMyy")); //TO DO : parse the file name pattern

            //TO DO get serial number - add rec if not found
            //TO do - insert a basic generic record
            string tmpFileName = fileName.Replace("{{Serial No}}", "");
            string serNo = SequenceGen.GetNextSequence(GetConnection(), GetSchema(), "generic", tmpFileName, 2, addIfNeeded: true, unlock: true);  //to do define const for generic
            fileName = fileName.Replace("{{Serial No}}", serNo);

            string[] args = { }; //DateTime.Now.ToString("dd-MMM-yyyy")  
            csvRep.CreateFile(bizTypeToRead, workdirYmd, fileName, args, paramsDict, whPart, ConstantBag.DET_LC_STEP_RESPONSE);           
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

    }

}
