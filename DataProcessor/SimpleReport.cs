using CommonUtil;
using DbOps;
using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class SimpleReport
    {
        private const string logProgramName = "Simple Report";
        private string pgSchema;
        private string pgConnection;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();

        public SimpleReport(string schemaName, string connectionStr)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;
        }

        public bool Print(string moduleName, string runFor, string fileType)
        {
            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, logProgramName, moduleName, 0 //JobId
            , out string systemConfigDir, out string inputRootDir, out string workDir);

            string workdirYmd = runFor;
            string bizTypeToRead = ConstantBag.LITE_IN;
            string waitingAction = "";   //all data
            string doneAction = ""; //all data
            string fileName = "all_";

            SetupFlagsForReports(fileType, ref waitingAction, ref doneAction, ref fileName);

            DataSet ds = DbUtil.GetInternalStatusReport(pgConnection, pgSchema, logProgramName, moduleName, bizTypeToRead, 0 //jobId
                , workdirYmd, waitingAction, doneAction, out string sql);
            if (ds == null || ds.Tables.Count < 1)
            {
                Logger.Write(logProgramName, "Print_int_stat", 0, fileType + "-No Table returned check sql", Logger.WARNING);
                Logger.Write(logProgramName, "Print_int_stat", 0, "sql:" + sql, Logger.WARNING);

                return false;
            }

            string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
                        + "\\" + workdirYmd// "yyyymmdd" 
                        + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
                        ;
            fileName = outputDir + "\\" + fileName + DateTime.Now.ToString("yyyyMMMdd_HH_mm") + ".csv";

            StreamWriter sw = new StreamWriter(fileName, true);
            char qt = '\"';
            char delimit = ',';
            string seprr = qt.ToString() + delimit + qt;
            //header
            string hdr = GetHeader();
            sw.WriteLine(hdr);
            for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
            {
                DataRow dr = ds.Tables[0].Rows[iRow];
                string courierId = Convert.ToString(dr["courier_id"]);
                //string pran = Convert.ToString(dr["prod_id"]);
                string docId = DbUtil.GetStringDbNullable(dr["docId"]);
                string fpath = DbUtil.GetStringDbNullable(dr["fpath"]);
                //fpath c:\...\lite\input\20210620\nps_lite - we want 20210620
                string[] dirSpl = fpath.Split(new char[] { '/', '\\' });
                string dateAsDir = dirSpl.Length >= 2 ? dirSpl[^2] : fpath;

                string actDone = DbUtil.GetStringDbNullable(dr["statact"]);
                if (actDone == "")
                    actDone = DbUtil.GetStringDbNullable(dr["updact"]);
                if (actDone == "")
                    actDone = DbUtil.GetStringDbNullable(dr["respact"]);
                if (actDone == "")
                    actDone = "waiting imm resp";

                string firstName = DbUtil.GetStringDbNullable(dr["fname"]);
                string lastName = DbUtil.GetStringDbNullable(dr["lname"]);
                string printDt = DbUtil.GetStringDbNullable(dr["print_dt"]);
                string pickupDt = DbUtil.GetStringDbNullable(dr["pickup_dt"]);
                string errCSV = DbUtil.GetStringDbNullable(dr["det_err_csv"]);
                errCSV = errCSV.Replace(",", "+");

                String det = qt.ToString() + dr[0] + seprr + dr[1] + seprr + courierId
                    + seprr + '\'' + dateAsDir
                    + seprr + '\'' + docId
                    + seprr + actDone
                    + seprr + CsvEscapeGreedy(firstName)
                    + seprr + CsvEscapeGreedy(lastName)
                    // leave the last 3 in that order - used by Update
                    + seprr + CsvEscapeGreedy(printDt)
                    + seprr + CsvEscapeGreedy(pickupDt)
                    + seprr + errCSV + qt;
                sw.WriteLine(det);
            }

            sw.Flush();

            return true;
        }

        private static void SetupFlagsForReports(string fileType, ref string waitingAction, ref string doneAction, ref string fileName)
        {
            if (fileType == "im_resp_todo") //internal report: immediate reponse not sent
            {
                waitingAction = ConstantBag.DET_LC_STEP_RESPONSE1;
                doneAction = "";
                fileName = "waiting_resp_";
            }
            if (fileType == "update_todo") //internal report: Final Status to do
            {
                waitingAction = ConstantBag.DET_LC_STEP_STAT_UPD2;
                doneAction = ConstantBag.DET_LC_STEP_RESPONSE1;
                fileName = "stat_todo_";
            }
            if (fileType == "status_todo") //internal report: Final Status to do
            {
                waitingAction = ConstantBag.DET_LC_STEP_STAT_REP3;
                doneAction = ConstantBag.DET_LC_STEP_STAT_UPD2;
                fileName = "stat_todo_";
            }
            if (fileType == "status_done") //internal report: Final Status done
            {
                waitingAction = "";
                doneAction = ConstantBag.DET_LC_STEP_STAT_REP3;
                fileName = "stat_done_";
            }
        }

        public static string GetHeader()
        {
            char qt = '\"';
            char delimit = ',';
            string seprr = qt.ToString() + delimit + qt;
            return $"{qt}Row Number{seprr}detail id{seprr}courier id{seprr}File Date{seprr}Document ID{seprr}Last Action{seprr}first name{seprr}last name{seprr}Print Dt{seprr}Pickup Dt{seprr}Status{qt}";
        }
        public static string CsvEscapeGreedy(string inStr)
        {
            string outStr = inStr.Replace(",", "")
                .Replace("\"", "\"\"");
            return outStr;
        }
        public static string CsvEscape(string inStr)
        {
            string outStr = inStr.Replace("\"", "\"\"");
            return outStr;
        }

    }
}
