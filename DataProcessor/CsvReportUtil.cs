using DbOps;
using DbOps.Structs;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class CsvReportUtil
    {
        private string pgConnection;
        private string pgSchema;
        private string moduleName;
        private string bizType;
        private int jobId;
        private RootJsonParamCSV csvConfig = null;
        private string outDir;

        private const string logProgramName = "CsvReportUtil";

        public RootJsonParamCSV GetCsvConfig()
        {
            return csvConfig;
        }

        public CsvReportUtil(string connection, string schema, string moduleNm, string bizTypeNm, int jobIdparam, string jsonDef, string outDirNm)
        {
            pgConnection = connection;
            pgSchema = schema;
            moduleName = moduleNm;
            bizType = bizTypeNm;
            jobId = jobIdparam;

            csvConfig = LoadJsonParamFile(jsonDef);
            outDir = outDirNm;
            if (outDir.EndsWith("/") == false)
                outDir = outDir + "/";
        }

        private RootJsonParamCSV LoadJsonParamFile(string jsonParamFilePath)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            RootJsonParamCSV csvConfig = JsonConvert.DeserializeObject<RootJsonParamCSV>(fileAsStr);

            SqlHelper.RemoveCommentedColumns(csvConfig.Header);
            SqlHelper.RemoveCommentedColumns(csvConfig.Detail);
            return csvConfig;
        }

        public bool CreateFile(string workdirYmd, string fileName, string[] progParams, Dictionary<string, string> paramsDict, DataSet ds, string waitingAction)
        {
            //get Data
            //string sql = SqlHelper.GetSelect(pgSchema, csvConfig.Detail, csvConfig.System, progParams);
            //DataSet ds = DbUtil.GetDataSet(pgConnection, bizType, logProgramName, jobId, sql);
            
            string fullOutFile = outDir + fileName;
            if (File.Exists(fullOutFile))
            {
                Logger.Write(logProgramName, "CreateFile", 0, "File Exists, appending:" + fullOutFile, Logger.WARNING);
            }
            else
            {
                Logger.Write(logProgramName, "CreateFile", 0, "Creating File:" + fullOutFile, Logger.INFO);
            }

            StreamWriter sw = new StreamWriter(fullOutFile, true);

            //print header
            CsvOutputHdrHandler hdrHandler = new CsvOutputHdrHandler();
            string hdr = hdrHandler.GetHeader(csvConfig.Header, ds, progParams, paramsDict, csvConfig.System.Delimt);
            sw.WriteLine(hdr);

            //print details
            CsvOutputDetHandler detHandler = new CsvOutputDetHandler();

            for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
            {
                String det = detHandler.GetDetRow(iRow, csvConfig.Detail, ds, progParams, csvConfig.System.Delimt);
                sw.WriteLine(det);
                //save the action for each iRow
                var dr = ds.Tables[0].Rows[iRow];
                int detailId = Convert.ToInt32(dr["detail_id"]);
                bool dbOk= DbUtil.AddAction(pgConnection, pgSchema, logProgramName, moduleName, jobId
                    ,iRow , detailId, waitingAction);
                if(dbOk == false)
                {
                    throw new Exception("DB ERROR: RERUN OR manually void/delete actions " + waitingAction + " work dir:" + workdirYmd);
                }
            }

            sw.Flush();

            return true;
        }
    }

}
