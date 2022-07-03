using CommonUtil;

namespace DataProcessor
{
    public class FileProcessorRegular : FileProcessor
    {
        public FileProcessorRegular(string connectionStr, string schemaName, string opName, string fileType) : base(connectionStr, schemaName, opName, fileType)
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

        public override void ProcessInput(string runFor, string deleteDir)
        {
            throw new System.NotImplementedException();
        }

        public override string GetBizTypeImageDirName(InputRecordAbs inputRecord)
        {
            throw new System.NotImplementedException();
        }

        public override ReportProcessor GetReportProcessor()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsMultifileJson()
        {
            throw new System.NotImplementedException();
        }
    }

}