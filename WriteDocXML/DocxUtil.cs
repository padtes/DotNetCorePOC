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
using System.Text.RegularExpressions;

namespace WriteDocXML
{
    public class DocxUtil
    {
        private string _pgSchema;
        private string _pgConnection;
        private RootJsonParamDocx _docxConfig = null;

        private string _templateXmlHdr;
        private string _templateXmlBody;
        private string _templateXmlFtr;

        private List<string> templateIds;

        private string _outDir;

        private const string moduleName = "DocxUtil";

        public DocxUtil(string pgConnection, string pgSchema, string jsonLetterDef, string templateFileName, string outDir)
        {
            _pgConnection = pgConnection;
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

            InitTemplateIds();
        }

        private void InitTemplateIds()
        {
            templateIds = new List<string>();
            string expr = @"\bw14:[\w]*(I|i)(d=)\S*";  //find w14:paraId="68B8078A" or w14:textId="77777777"

            MatchCollection mc = Regex.Matches(_templateXmlBody, expr);
            foreach (Match m in mc)
            {
                if (templateIds.Contains(m.Value) == false)
                    templateIds.Add(m.Value);
            }
        }

        public bool CreateMultiPageFiles(int jobId, int mergeCount, string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            Logger.WriteInfo(moduleName, "CreateMultiPageFiles", 0, "All file Create Started, max pages per file:" + mergeCount);

            try
            {
                int curCount = 0;
                int fileCount = 0;
                stopwatch.Start();
                List<string> usedIds = new List<string>();

                string sql = SqlHelper.GetSelect(_pgSchema, _docxConfig.Placeholders, _docxConfig.System, args); //to do where condition
                //to do order by
                DataSet ds = DbUtil.GetDataSet(_pgConnection, moduleName, jobId, sql);
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
                    AddMidSection(sbMidSect, tokenMap, curCount, mergeCount, remainCount, usedIds);
                    hasUnPrinted = true;

                    if (curCount == mergeCount)
                    {
                        WriteDocumentXmlFile(sbMidSect, ref fileCount);
                        curCount = 0;
                        sbMidSect.Clear();
                        usedIds.Clear();
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

        private void AddMidSection(StringBuilder sbMidSect, List<KeyValuePair<string, string>> tokenMap, int curCount, int mergeCount, int remainCount, List<string> usedIds)
        {
            String sTemplate = new String(_templateXmlBody);
            foreach (var tokenVal in tokenMap)
            {
                sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
            }

            foreach (string idType in templateIds)
            {
                string newIdType = StrUtil.GetNewKeyVal(idType, usedIds);
                sTemplate = sTemplate.Replace(idType, newIdType);
            }
            sbMidSect.Append(sTemplate);

            if (curCount < mergeCount && remainCount > 0)  //except last page - last file's last page
            {
                sbMidSect.Append("<w:br w:type = \"page\" />"); //page break

                //sbMidSect.Append("<w:lastRenderedPageBreak/>");  //page break
            }
        }

        private void FillTokenMap(List<KeyValuePair<string, string>> tokenMap, DataRow dr)
        {
            for (int i = 0; i < _docxConfig.Placeholders.Count; i++)
            {
                ColumnDetail phCol = _docxConfig.Placeholders[i];
                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                    //to do interprete c-function
                    continue; //C Functions are not part of sql select

                string dbVal = "";
                if (dr[i] != DBNull.Value)
                {
                    dbVal = Convert.ToString(dr[i]);
                }

                tokenMap.Add(new KeyValuePair<string, string>(phCol.Tag, dbVal));
            }
        }

        public bool CreateAllSeparateFiles(int jobId, string[] args)
        {
            try
            {
                string sql = SqlHelper.GetSelect(_pgSchema, _docxConfig.Placeholders, _docxConfig.System, args); //to do where condition

                //to do order by
                DataSet ds = DbUtil.GetDataSet(_pgConnection, moduleName, jobId, sql);
                if (ds == null || ds.Tables.Count < 1)
                    return false;

                List<KeyValuePair<string, string>> tokenMap = new List<KeyValuePair<string, string>>();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    for (int i = 0; i < _docxConfig.Placeholders.Count; i++)
                    {
                        ColumnDetail phCol = _docxConfig.Placeholders[i];
                        if (phCol.SrcType.ToUpper() == "CFUNCTION")
                            //to do interprete c-function
                            continue; //C Functions are not part of sql select

                        string dbVal = "";
                        if (dr[i] != DBNull.Value)
                        {
                            dbVal = Convert.ToString(dr[i]);
                        }
                        if (i == 0 && dbVal == "") //ASSUMED key to be at 0 in JSON placeholders
                            throw new Exception("0 th column expected to be document key for Letters. " + phCol.Tag);  //do not use the record

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

        public static RootJsonParamDocx LoadJsonParamFile(string jsonParamFilePath)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            RootJsonParamDocx docxConfig = JsonConvert.DeserializeObject<RootJsonParamDocx>(fileAsStr);
            SqlHelper.RemoveCommentedColumns(docxConfig.Placeholders);

            return docxConfig;
        }

    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class CommentsOnUsageDocx
    {
        public string _0 { get; set; }
        public string _1 { get; set; }
        public string _2 { get; set; }
    }

    public class RootJsonParamDocx
    {
        [JsonProperty("comments_on_usage")]
        public CommentsOnUsageDocx CommentsOnUsage { get; set; }

        [JsonProperty("system")]
        public SystemParam System { get; set; }

        [JsonProperty("placeholders")]
        public List<ColumnDetail> Placeholders { get; set; }
    }



}
