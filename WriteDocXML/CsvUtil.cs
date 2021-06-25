using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace WriteDocXML
{
    public class CsvUtil
    {
        private string _pgSchema;
        private string _pgConnection;
        private RootJsonParamCSV _csvConfig = null;
        private string _outDir;

        private const string moduleName = "CsvUtil";

        public CsvUtil(string pgConnection, string pgSchema, string bizType, string jsonDef, string outDir)
        {
            _pgConnection = pgConnection;
            _pgSchema = pgSchema;
            _csvConfig = LoadJsonParamFile(jsonDef);
            _outDir = outDir;
            if (outDir.EndsWith("/") == false)
                _outDir = _outDir + "/";
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

        public bool CreateFile(string bizType, string fileName, string[] progParams, int jobId)
        {
            //get Data
            string sql = SqlHelper.GetSelect(_pgSchema, _csvConfig.Detail, _csvConfig.System, progParams);

            DataSet ds = DbUtil.GetDataSet(_pgConnection, bizType, moduleName, jobId, sql);
            if (ds == null || ds.Tables.Count < 1)
            {
                Logger.Write(moduleName, "CreateFile", 0, "No Table returned check sql", Logger.WARNING);
                Logger.Write(moduleName, "CreateFile", 0, "sql:" + sql, Logger.WARNING);

                return false;
            }

            string fullOutFile = _outDir + fileName;
            StreamWriter sw = new StreamWriter(fullOutFile, false);

            string delimt = _csvConfig.System.Delimt;

            //print header
            CsvOutputHdrHandler hdrHandler = new CsvOutputHdrHandler();
            string hdr = hdrHandler.GetHeader(_csvConfig.Header, ds, progParams, _csvConfig.System.Delimt);
            sw.WriteLine(hdr);

            //print details
            CsvOutputDetHandler detHandler = new CsvOutputDetHandler();

            for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
            {
                String det = detHandler.GetDetRow(iRow, _csvConfig.Detail, ds, progParams, _csvConfig.System.Delimt);
                sw.WriteLine(det);
            }

            sw.Flush();

            return true;
        }

    }


    public class CommentsOnUsageCSV
    {
        [JsonProperty("0")]
        public string _0 { get; set; }

        [JsonProperty("1")]
        public string _1 { get; set; }

        [JsonProperty("2")]
        public string _2 { get; set; }

        [JsonProperty("3")]
        public string _3 { get; set; }

        [JsonProperty("3.1")]
        public string _31 { get; set; }

        [JsonProperty("3.2a")]
        public string _32a { get; set; }

        [JsonProperty("3.2b")]
        public string _32b { get; set; }

        [JsonProperty("3.2c")]
        public string _32c { get; set; }

        [JsonProperty("3.3")]
        public string _33 { get; set; }
    }

    public class SystemParamCSV : SystemParam
    {
        [JsonProperty("delimt")]
        public string Delimt { get; set; }
    }

    public class RootJsonParamCSV
    {
        [JsonProperty("comments_on_usage")]
        public CommentsOnUsageCSV CommentsOnUsage { get; set; }

        [JsonProperty("system")]
        public SystemParamCSV System { get; set; }

        [JsonProperty("header")]
        public List<ColumnDetail> Header { get; set; }

        [JsonProperty("detail")]
        public List<ColumnDetail> Detail { get; set; }
    }

    public class CsvOutputHandler
    {
        public string GetVal(ColumnDetail columnDetail, DataSet detailRowDS, int rowInd, string[] progParams, CommandHandler cmdHandler)
        {
            string val = columnDetail.DbValue; //default / "CONST"

            switch (columnDetail.SrcType.ToUpper())
            {
                case "PARAM":
                    val = SqlHelper.GetParamValue(progParams, columnDetail).TrimEnd('\'').TrimStart('\'');
                    break;
                case "CFUNCTION":
                    bool isConst;
                    val = cmdHandler.Handle(columnDetail.DbValue, progParams, detailRowDS.Tables[0].Rows[rowInd], out isConst);
                    if (isConst)
                    {
                        columnDetail.SrcType = "const";
                        columnDetail.DbValue = val;
                    }
                    break;
                case "COLUMN":
                    string valIndxStr = columnDetail.DbValue;
                    DataRow dr = detailRowDS.Tables[0].Rows[rowInd];
                    val = string.Empty;
                    int valIndx;
                    if (int.TryParse(valIndxStr, out valIndx))
                    {
                        if (dr[valIndx] != DBNull.Value)
                            val = Convert.ToString(dr[valIndx]);
                    }
                    else
                    {
                        if (dr[valIndxStr] != DBNull.Value)
                            val = Convert.ToString(dr[valIndxStr]);
                    }
                    break;
                default:
                    break;
            }

            return val;
        }
    }

    public class CsvOutputHdrHandler : CsvOutputHandler
    {
        public string GetHeader(List<ColumnDetail> headerColumns, DataSet detailRowDS, string[] progParams, string delimit)
        {
            String hdr = "";
            //var tbl = detailRowDS.Tables[0];
            CommandHandler cmdHandler = new CommandHandler();

            for (int i = 0; i < headerColumns.Count; i++)
            {
                string hdVal = GetVal(headerColumns[i], detailRowDS, 0, progParams, cmdHandler);
                if (i > 0)
                {
                    hdr += delimit;
                }

                hdr += hdVal;
            }

            return hdr;
        }
    }

    public class CsvOutputDetHandler : CsvOutputHandler
    {
        internal string GetDetRow(int iRow, List<ColumnDetail> detailColumns, DataSet ds, string[] progParams, string delimt)
        {
            String det = "";
            int cellInd = 0;
            bool isFirst = true;
            bool isConst;
            string val;
            CommandHandler cmdHandler = new CommandHandler();
            DataRow dr = ds.Tables[0].Rows[iRow];

            for (int i = 0; i < detailColumns.Count; i++)
            {
                if (isFirst == false)
                    det += delimt;

                val = string.Empty;

                if (detailColumns[i].SrcType.ToUpper() == "CFUNCTION")
                {
                    val = cmdHandler.Handle(detailColumns[i].DbValue, progParams, dr, out isConst);
                }
                else 
                {
                    if (dr[cellInd] != DBNull.Value)
                    {
                        val = Convert.ToString(dr[cellInd]);
                    }
                    cellInd++;
                }

                det += val;
                isFirst = false;
            }
            return det;
        }
    }

}
