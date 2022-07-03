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
        public void SetCsvConfig(RootJsonParamCSV val)
        {
            this.csvConfig = val;
        }

        public CsvReportUtil(string connection, string schema, string moduleNm, string bizTypeNm, int jobIdparam, string jsonDef, string outDirNm)
        {
            pgConnection = connection;
            pgSchema = schema;
            moduleName = moduleNm;
            bizType = bizTypeNm;
            jobId = jobIdparam;

            if (jsonDef != string.Empty)
                csvConfig = LoadJsonParamFile(jsonDef);
            outDir = outDirNm;
            if (outDir.EndsWith("/") == false)
                outDir = outDir + "/";
        }

        public static RootJsonParamCSV LoadJsonParamFile(string jsonParamFilePath)
        {
            Logger.Write(logProgramName, "LoadJsonParamFile", 0, "Reading CSV report params:" + jsonParamFilePath, Logger.INFO);

            try
            {
                StreamReader sr = new StreamReader(jsonParamFilePath);
                string fileAsStr = sr.ReadToEnd();

                RootJsonParamCSV csvConfig = JsonConvert.DeserializeObject<RootJsonParamCSV>(fileAsStr);

                SqlHelper.RemoveCommentedColumns(csvConfig.Header);
                SqlHelper.RemoveCommentedColumns(csvConfig.Detail);
                return csvConfig;
            }
            catch (Exception)
            {
                Logger.Write(logProgramName, "LoadJsonParamFile", 0, "Error parsing:" + jsonParamFilePath, Logger.ERROR);
                throw;
            }
        }

        public bool CreateFile(string workdirYmd, string fileName, string subDir, string[] progParams, Dictionary<string, string> paramsDict, DataSet ds, string waitingAction)
        {
            //get Data
            //string sql = SqlHelper.GetSelect(pgSchema, csvConfig.Detail, csvConfig.System, progParams);
            //DataSet ds = DbUtil.GetDataSet(pgConnection, bizType, logProgramName, jobId, sql);

            string fullOutFile = outDir;
            if (string.IsNullOrEmpty(subDir) == false)
            {
                fullOutFile = Path.Combine(outDir, subDir);
            }
            if (Directory.Exists(fullOutFile) == false)
                Directory.CreateDirectory(fullOutFile);

            fullOutFile = Path.Combine(fullOutFile, fileName);

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
            string hdr = hdrHandler.GetHeader(csvConfig.Header, ds, progParams, paramsDict
                , csvConfig.System.Delimt, csvConfig.System.TextQualifier, csvConfig.System.EscQualifier);
            sw.WriteLine(hdr);

            int numOfBlanks = csvConfig.System.GetNumberOfBlankLines();
            if (numOfBlanks > 0)
            {
                WriteBlankLines(sw, hdr, numOfBlanks);
            }

            //print details
            CsvOutputDetHandler detHandler = new CsvOutputDetHandler();

            for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
            {
                String det = detHandler.GetDetRow(iRow, csvConfig.Detail, ds, progParams, paramsDict
                    , csvConfig.System.Delimt, csvConfig.System.TextQualifier, csvConfig.System.EscQualifier);

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

        private void WriteBlankLines(StreamWriter sw, string hdr, int numOfBlanks)
        {
            char cd = char.Parse(csvConfig.System.Delimt);
            string[] tmp2 = hdr.Split(cd);
            string tmpCSV = new string(cd, tmp2.Length - 1);

            for (int i = 0; i < numOfBlanks; i++)
            {
                sw.WriteLine(tmpCSV);
            }
        }
    }

}
