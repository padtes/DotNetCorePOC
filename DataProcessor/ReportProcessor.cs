using CommonUtil;
using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessor
{
    public abstract class ReportProcessor
    {
        protected string pgConnection;
        protected string pgSchema;
        protected string moduleName;
        protected string operation;
        public int JobId { get; set; }

        protected string systemConfigDir;
        protected string inputRootDir;
        protected string workDir;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();
        protected List<string> staticParamList = new List<string>();

        public ReportProcessor(string connectionStr, string schemaName, string module, string opName)
        {
            pgConnection = connectionStr;
            pgSchema = schemaName;
            moduleName = module;
            operation = opName;
        }
        public abstract string GetProgName();
        public abstract string GetBizType();
        protected abstract void LoadModuleParam(string runFor, string courierCsv);
        public abstract void WriteOutput(string runFor, string courierCsv);

        public virtual void ProcessOutput(string runFor, string courierCsv)
        {
            LoadModuleParam(runFor, courierCsv);
            WriteOutput(runFor, courierCsv);
        }

        public string GetSchema() { return pgSchema; }
        public string GetConnection() { return pgConnection; }
    }

    public class ReportProcessorLite : ReportProcessor
    {
        public ReportProcessorLite(string connectionStr, string schemaName, string module, string opName) : base(connectionStr, schemaName, module, opName)
        {
        }

        public override string GetProgName() {
            return "ReportProcessorLite";
        }
        public override string GetBizType()
        {
            return ConstantBag.LITE_OUT_RESPONSE;
        }
        public override void WriteOutput(string runFor, string courierCsv)
        {
            if (runFor == "ir") //immdeiate resp
            {
                ProcessNpsApyLiteOutputImmResp(courierCsv);
            }
            else
            {
                ProcessNpsLiteOutput(runFor, courierCsv);

                ProcessApyOutput(runFor, courierCsv);
            }
        }

        private void ProcessNpsApyLiteOutputImmResp(string courierCcsv)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                --- workDir / ddmmyyyy / nps_lite_apy / nps 
                --- workDir / ddmmyyyy / nps_lite_apy / nps / <status file> <response file>
             */

            string irDir = "";
            string fileName = "";

            //DbUtil.GetFileInfoList

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
            staticParamList = new List<string>() { ConstantBag.PARAM_OUTPUT_PARENT_DIR, ConstantBag.PARAM_OUTPUT_LITE_DIR, ConstantBag.PARAM_OUTPUT_APY_DIR};

            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, GetProgName(), moduleName, JobId
                , out systemConfigDir, out inputRootDir, out workDir);

            ProcessorUtil.ValidateStaticParam(moduleName, GetBizType(), GetProgName(), paramsDict, staticParamList);
        }

    }

}
