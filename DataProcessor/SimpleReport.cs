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

            if (fileType == "int_stat") //internal status
            {
                string workdirYmd = runFor;
                string bizTypeToRead = ConstantBag.LITE_IN;
                string waitingAction = ConstantBag.DET_LC_STEP_STATUS;
                string doneAction = ConstantBag.DET_LC_STEP_RESPONSE;

                DataSet ds = DbUtil.GetInternalStatusReport(pgConnection, pgSchema, logProgramName, moduleName, bizTypeToRead, 0 //jobId
                    , workdirYmd, waitingAction, doneAction, out string sql);

                string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
                            + "\\" + workdirYmd// "yyyymmdd" 
                            + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
                            ;
                string fileName = outputDir + @"\internalStat_" + DateTime.Now.ToString("yyyyMMMdd_HH_mm") + ".csv";

                if (ds == null || ds.Tables.Count < 1)
                {
                    Logger.Write(logProgramName, "Print_int_stat", 0, "No Table returned check sql", Logger.WARNING);
                    Logger.Write(logProgramName, "Print_int_stat", 0, "sql:" + sql, Logger.WARNING);

                    return false;
                }

                StreamWriter sw = new StreamWriter(fileName, true);
                char qt = '\"';
                char delimit = ',';
                string seprr = qt.ToString() + delimit + qt;
                //header
                string hdr = $"{qt}Row Number{seprr}detail id{seprr}courier id{seprr}PRAN{seprr}first name{seprr}last name{seprr}Status{qt}";
                sw.WriteLine(hdr);
                for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
                {
                    DataRow dr = ds.Tables[0].Rows[iRow];
                    string courierId = Convert.ToString(dr["courier_id"]);
                    string pran = Convert.ToString(dr["prod_id"]);
                    string fname = DbUtil.GetStringDbNullable(dr["fname"]);
                    string lname = DbUtil.GetStringDbNullable(dr["lname"]);
                    string errCSV = DbUtil.GetStringDbNullable(dr["det_err_csv"]);
                    errCSV = errCSV.Replace(",", "+");

                    String det = qt.ToString() + dr[0] + seprr + dr[1] + seprr + courierId 
                        + seprr + '\'' + pran 
                        + seprr + CsvEscape(fname)
                        + seprr + CsvEscape(lname)
                        + seprr + errCSV + qt; 
                    sw.WriteLine(det);
                }

                sw.Flush();
            }
            return true;
        }

        public static string GetHeader()
        {
            char qt = '\"';
            char delimit = ',';
            string seprr = qt.ToString() + delimit + qt;
            return $"{qt}Row Number{seprr}detail id{seprr}courier id{seprr}PRAN{seprr}first name{seprr}last name{seprr}Status{qt}";
        }
        public static string CsvEscape(string inStr)
        {
            string outStr = inStr.Replace(",","")
                .Replace("\"", "\"\"");
            return outStr;
        }

    }
}
