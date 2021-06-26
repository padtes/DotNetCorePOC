using CommonUtil;

namespace NpsApy
{
    internal class FileProcessorRegular : FileProcessor
    {
        public FileProcessorRegular(string schemaName, string connectionStr) : base(schemaName, connectionStr)
        {

        }

        public override string GetModuleName()
        {
            return ConstantBag.MODULE_REG;
        }

        public override bool ProcessModule(string operation, string runFor, string courierCsv)
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
    }

}