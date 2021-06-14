using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace WriteDocXML
{
    public class DocxUtil
    {
        private string _templateXmlAsStr;
        private string _outDir;
        private const string moduleName = "DocxUtil";

        public DocxUtil(string templateFileName, string outDir)
        {
            using (StreamReader sr = new StreamReader(templateFileName))
            {
                _templateXmlAsStr = sr.ReadToEnd();
            }
            _outDir = outDir;
        }

        public bool CreateFile(string fileName, List<KeyValuePair<string, string>> tokenMap)
        {
            try
            {
                String sTemplate = new String(_templateXmlAsStr);
                foreach (var tokenVal in tokenMap)
                {
                    sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
                }

                string fullOutFile = _outDir + "/" + fileName;
                StreamWriter sw = new StreamWriter(fullOutFile, false);
                sw.Write(sTemplate);
                sw.Flush();

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "CreateFile", 0, ex);
                return false;
            }
        }

        public static RootJsonParam LoadJsonParamFile(string jsonParamFilePath)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            RootJsonParam docxConfig = JsonConvert.DeserializeObject<RootJsonParam>(fileAsStr);

            return docxConfig;
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
        public string file_type { get; set; }
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
        public string SourceType { get; set; }
        [JsonProperty("db_value")]
        public string DbValue { get; set; }
        [JsonProperty("alias")]
        public string Alias { get; set; }
    }

    public class RootJsonParam
    {
        public CommentsOnUsage comments_on_usage { get; set; }
        public SystemParam system { get; set; }
        public List<Placeholder> placeholders { get; set; }
    }



}
