namespace NpsApy
{
    internal class FileProcessorRegular : FileProcessor
    {
        public FileProcessorRegular(string schemaName, string connectionStr) : base(schemaName, connectionStr)
        {

        }

        public override string GetBizType()
        {
            return BIZ_REG;
        }

        public override bool ProcessBiz(string operation, string runFor, string courierCsv)
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