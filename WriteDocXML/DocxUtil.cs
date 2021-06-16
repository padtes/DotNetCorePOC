using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text;
using System.Data;
using DbOps;
using System.Diagnostics;

namespace WriteDocXML
{
    public class DocxUtil
    {
        private string _pgSchema;
        private string _pgConStr;
        private RootJsonParam _docxConfig = null;

        private static string _templateXmlHdr;
        private static string _templateXmlBody;
        private static string _templateXmlFtr;

        private string _outDir;

        private const string moduleName = "DocxUtil";

        public DocxUtil(string pgConStr, string pgSchema, string jsonLetterDef, string templateFileName, string outDir)
        {
            _pgConStr = pgConStr;
            _pgSchema = pgSchema;
            _docxConfig = LoadJsonParamFile(jsonLetterDef);

            string templateXmlAsStr = "";
            using (StreamReader sr = new StreamReader(templateFileName))
            {
                templateXmlAsStr = sr.ReadToEnd();
            }

            string tmpSect1End = "<w:body>";
            string tmpSect2End = "</w:body>";

            int bodyIndx = templateXmlAsStr.IndexOf(tmpSect1End);
            if (bodyIndx < 1)
            {
                throw new Exception(tmpSect1End + " tag not found");
            }
            _templateXmlHdr = templateXmlAsStr.Substring(0, bodyIndx + tmpSect1End.Length);

            int endBdIndx = templateXmlAsStr.IndexOf(tmpSect2End);
            _templateXmlBody = templateXmlAsStr.Substring(bodyIndx + tmpSect1End.Length, endBdIndx - bodyIndx - tmpSect1End.Length);
            _templateXmlFtr = templateXmlAsStr.Substring(endBdIndx);
            _outDir = outDir;
        }

        public bool CreateMultiPageFiles(int jobId, int mergeCount)
        {
            Stopwatch stopwatch = new Stopwatch();
            Logger.WriteInfo(moduleName, "CreateMultiPageFiles", 0, "All file Create Started, max pages per file:" + mergeCount);

            try
            {
                int curCount = 0;
                int fileCount = 0;
                stopwatch.Start();

                string sql = GetSelect(); //to do where condition
                //to do order by
                DataSet ds = DbUtil.GetDataSet(_pgConStr, moduleName, jobId, sql);
                if (ds == null || ds.Tables.Count < 1)
                {
                    Logger.Write(moduleName, "CreateMultiPageFiles", 0, "No Table returned check sql", Logger.WARNING);
                    Logger.Write(moduleName, "CreateMultiPageFiles", 0, "sql:" + sql, Logger.WARNING);

                    return false;
                }

                StringBuilder sbMidSect = new StringBuilder();
                bool hasUnPrinted = false;
                int remainCount = ds.Tables[0].Rows.Count;

                Logger.WriteInfo(moduleName, "CreateMultiPageFiles", 0, "Record count to process:" + remainCount);

                List<KeyValuePair<string, string>> tokenMap = new List<KeyValuePair<string, string>>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    curCount++;
                    remainCount--;

                    FillTokenMap(tokenMap, dr);
                    AddMidSection(sbMidSect, tokenMap, curCount, mergeCount, remainCount);
                    hasUnPrinted = true;

                    if (curCount == mergeCount)
                    {
                        WriteDocumentXmlFile(sbMidSect, ref fileCount);
                        curCount = 0;
                        sbMidSect.Clear();
                        hasUnPrinted = false;
                    }

                    tokenMap.Clear();
                }

                if (hasUnPrinted)
                {
                    WriteDocumentXmlFile(sbMidSect, ref fileCount);
                }

                stopwatch.Stop();
                Logger.WriteInfo(moduleName, "CreateMultiPageFiles", 0, "All files [" + fileCount + "] Created. Time taken in sec:" + stopwatch.Elapsed.TotalSeconds);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "CreateMultiPageFiles", 0, ex);
                return false;
            }
        }

        private void WriteDocumentXmlFile(StringBuilder sbMidSect, ref int fileCount)
        {
            fileCount++;

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullOutFile = _outDir + "/document_" + ts + "_" + fileCount + ".xml";
            StreamWriter sw = new StreamWriter(fullOutFile, false);

            sbMidSect.Append(_templateXmlFtr);

            string sFull = _templateXmlHdr + sbMidSect.ToString(); 
            sw.Write(sFull);

            sw.Flush();
        }

        private void AddMidSection(StringBuilder sbMidSect, List<KeyValuePair<string, string>> tokenMap, int curCount, int mergeCount, int remainCount)
        {
            String sTemplate = new String(_templateXmlBody);
            foreach (var tokenVal in tokenMap)
            {
                sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
            }
            sbMidSect.Append(sTemplate);

            if (curCount < mergeCount && remainCount > 0)  //except last page - last file's last page
            {
                sbMidSect.Append("<w:lastRenderedPageBreak/>");  //page break
            }
        }

        private void FillTokenMap(List<KeyValuePair<string, string>> tokenMap, DataRow dr)
        {
            for (int i = 0; i < _docxConfig.Placeholders.Count; i++)
            {
                Placeholder phCol = _docxConfig.Placeholders[i];
                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                    //to do interprete c-function
                    continue; //C Functions are not part of sql select

                string dbVal = "";
                if (dr[i] != DBNull.Value)
                {
                    dbVal = Convert.ToString(dr[i]);
                }
                if (i == 0 && dbVal == "") //ASSUMED key to be at 0 in JSON placeholders
                    break;  //do not use the record

                tokenMap.Add(new KeyValuePair<string, string>(phCol.Tag, dbVal));
            }
        }

        public bool CreateAllSeparateFiles(int jobId)
        {
            try
            {
                string sql = GetSelect(); //to do where condition
                //to do order by
                DataSet ds = DbUtil.GetDataSet(_pgConStr, moduleName, jobId, sql);
                if (ds == null || ds.Tables.Count < 1)
                    return false;

                List<KeyValuePair<string, string>> tokenMap = new List<KeyValuePair<string, string>>();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    for (int i = 0; i < _docxConfig.Placeholders.Count; i++)
                    {
                        Placeholder phCol = _docxConfig.Placeholders[i];
                        if (phCol.SrcType.ToUpper() == "CFUNCTION")
                            //to do interprete c-function
                            continue; //C Functions are not part of sql select

                        string dbVal = "";
                        if (dr[i] != DBNull.Value)
                        {
                            dbVal = Convert.ToString(dr[i]);
                        }
                        if (i == 0 && dbVal == "") //ASSUMED key to be at 0 in JSON placeholders
                            break;  //do not use the record

                        tokenMap.Add(new KeyValuePair<string, string>(phCol.Tag, dbVal));
                    }

                    string pranId = Convert.ToString(dr[0]); //ASSUMED proan_id to be at 0 in JSON placeholders
                    if (pranId != "")
                        CreateFile("Document_" + pranId + ".xml", tokenMap);

                    tokenMap.Clear();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "CreateAllSeparateFiles", 0, ex);
                return false;
            }
        }

        public void CreateFile(string fileName, List<KeyValuePair<string, string>> tokenMap)
        {
            String sTemplate = new String(_templateXmlHdr + _templateXmlBody + _templateXmlFtr);
            foreach (var tokenVal in tokenMap)
            {
                sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
            }

            string fullOutFile = _outDir + "/" + fileName;
            StreamWriter sw = new StreamWriter(fullOutFile, false);
            sw.Write(sTemplate);
            sw.Flush();
        }

        public static RootJsonParam LoadJsonParamFile(string jsonParamFilePath)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            RootJsonParam docxConfig = JsonConvert.DeserializeObject<RootJsonParam>(fileAsStr);

            return docxConfig;
        }

        private string GetSelect()
        {
            bool first = true;
            StringBuilder sql = new StringBuilder("select ");

            for (int i = 0; i < _docxConfig.Placeholders.Count; i++)
            {
                Placeholder phCol = _docxConfig.Placeholders[i];
                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                    continue; //C Functions are not part of sql select

                if (first == false)
                {
                    sql.Append(", ");
                }
                switch (phCol.SrcType.ToUpper())
                {
                    case "JSON":
                        sql.Append(_docxConfig.System.DataTableJsonCol).Append("->");
                        sql.Append(phCol.DbValue);
                        break;
                    default:  //"COLUMN" / "SQLFUNCTION"
                        sql.Append(phCol.DbValue);
                        break;
                }
                first = false;
            }
            sql.Append(" from ")
                .Append(_pgSchema).Append('.')
                .Append(_docxConfig.System.DataTableName);

            //to do where 

            sql.Append(';');

            return sql.ToString();
        }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class CommentsOnUsage
    {
        public string _0 { get; set; }
        public string _1 { get; set; }
        public string _2 { get; set; }
    }

    [JsonObject("System")]
    public class SystemParam
    {
        [JsonProperty("file_type")]
        public string FileType { get; set; }

        [JsonProperty("data_table_name")]
        public string DataTableName { get; set; }

        [JsonProperty("data_table_json_col")]
        public string DataTableJsonCol { get; set; }
    }

    public class Placeholder
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("src_type")]
        public string SrcType { get; set; }

        [JsonProperty("db_value")]
        public string DbValue { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }
    }

    public class RootJsonParam
    {
        [JsonProperty("comments_on_usage")]
        public CommentsOnUsage CommentsOnUsage { get; set; }

        [JsonProperty("system")]
        public SystemParam System { get; set; }

        [JsonProperty("placeholders")]
        public List<Placeholder> Placeholders { get; set; }
    }



}
