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
        private string _pgConStr;
        private RootJsonParamCSV _csvConfig = null;
        private string _outDir;

        private const string moduleName = "CsvUtil";

        public CsvUtil(string pgConStr, string pgSchema, string jsonDef, string outDir)
        {
            _pgConStr = pgConStr;
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

        public bool CreateFile(string fileName, string[] args, int jobId)
        {
            //get Data
            string sql = SqlHelper.GetSelect(_pgSchema, _csvConfig.Detail, _csvConfig.System, args);

            DataSet ds = DbUtil.GetDataSet(_pgConStr, moduleName, jobId, sql);
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
            string hdr = "To do-hdr";
            sw.WriteLine(hdr);

            //print details
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow dr = ds.Tables[0].Rows[i];
                String det = "";

                for (int j = 0; j < ds.Tables[0].Columns.Count; j++)
                {
                    if (j > 0)
                    {
                        det += delimt;
                    }

                    if (dr[j] != DBNull.Value)
                    {
                        det += Convert.ToString(dr[j]);
                    }
                }
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


}
