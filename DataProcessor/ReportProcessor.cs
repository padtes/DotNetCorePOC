using DbOps.Structs;
using Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
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
        public abstract void WriteOutput(string runFor, string courierCsv, string fileType);

        public abstract DataSet GetReportDS(string pgConnection, string pgSchema, string moduleName, string bizTypeToRead, string bizTypeToWrite, int jobId
            , RootJsonParamCSV csvConfig, string[] progParams, string workdirYmd, string wherePart);

        public virtual void ProcessOutput(string runFor, string courierCsv, string fileType)
        {
            Logger.WriteInfo(GetProgName(), "ProcessOutput", 0
                , $"START {moduleName} op:{operation} fileType:{fileType} parameters: {runFor} {(courierCsv == "" ? "" : " courier:" + courierCsv)} ");

            LoadModuleParam(runFor, courierCsv);
            WriteOutput(runFor, courierCsv, fileType);
        }

        public string GetSchema() { return pgSchema; }
        public string GetConnection() { return pgConnection; }
    }

}
