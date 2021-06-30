using CommonUtil;

namespace DataProcessor
{
    public class FileProcessorRegular : FileProcessor
    {
        public FileProcessorRegular(string schemaName, string connectionStr) : base(schemaName, connectionStr)
        {

        }

        public override string GetModuleName()
        {
            return ConstantBag.MODULE_REG;
        }

        protected override void LoadModuleParam(string operation, string runFor, string courierCsv)
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessInput(string runFor)
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessOutput(string runFor, string courierCcsv)
        {
            throw new System.NotImplementedException();
        }

        public override string GetBizTypeDirName(InputRecordAbs inputRecord)
        {
            throw new System.NotImplementedException();
        }
    }

}