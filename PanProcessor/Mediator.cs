
using CommonUtil;

namespace PanProcessor
{
    public class Mediator
    {
        public static bool ProcessPAN(string pgConnection, string pgSchema, string operation, string modType, string runFor, string courierCSV, string fileType, string deleteDir)
        {
            bool runResult = false;

            if (operation == "report")
            {
                //to do summary REPORT will dump simple report of counts by <date>, < COURIER >, count of yet to print cards or in records in error
                //to do courier REPORT will dump < COURIER >, range - from-to and next
            }
            else
            if (operation == "read")
            {
                if (fileType == "" || fileType == "all")
                {
                    runResult = PanBizProcess(pgConnection, pgSchema, operation, runFor, courierCSV, fileType: ConstantBag.PAN_INDIV, deleteDir);
                    runResult &= PanBizProcess(pgConnection, pgSchema, operation, runFor, courierCSV, fileType:ConstantBag.PAN_CORP, deleteDir);
                    runResult &= PanBizProcess(pgConnection, pgSchema, operation, runFor, courierCSV, fileType:ConstantBag.PAN_EKYC, deleteDir);
                }
                else
                    runResult = PanBizProcess(pgConnection, pgSchema, operation, runFor, courierCSV, fileType, deleteDir);

            }
            else
            if (operation == "write")
            {
                //PanFileProcessor processor = new PanFileProcessor(pgConnection, pgSchema, operation, fileType);

                //runResult = processor.ProcessModule(operation, runFor, courierCSV, fileType, deleteDir);
            }
            //TO DO handle unknown operation

            return runResult;
        }

        private static bool PanBizProcess(string pgConnection, string pgSchema, string operation, string runFor, string courierCSV, string fileType, string deleteDir)
        {
            bool runResult;
            PanFileProcessor processor = new PanFileProcessor(pgConnection, pgSchema, operation, fileType);
            runResult = processor.ProcessModule(operation, runFor, courierCSV, fileType, deleteDir);
            return runResult;
        }
    }
}
