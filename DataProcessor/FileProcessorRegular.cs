using CommonUtil;

namespace DataProcessor
{
    public class FileProcessorRegular : FileProcessor
    {
        public FileProcessorRegular(string connectionStr, string schemaName, string opName) : base(connectionStr, schemaName, opName)
        {

        }

        public override string GetModuleName()
        {
            return ConstantBag.MODULE_REG;
        }

        protected override void LoadModuleParam(string runFor, string courierCsv)
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessInput(string runFor)
        {
            throw new System.NotImplementedException();
        }

        public override string GetBizTypeDirName(InputRecordAbs inputRecord)
        {
            throw new System.NotImplementedException();
        }

        public override ReportProcessor GetReportProcessor(string operation)
        {
            throw new System.NotImplementedException();
        }
    }

}