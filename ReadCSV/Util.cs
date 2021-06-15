using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Logging;
using System.Threading.Tasks;
using DbOps;

namespace ReadCSV
{
    public class Util
    {
        private static readonly string moduleName = "Util";
        //Hostname: qm20.siteground.biz Username: Lsg@arubapalmsrealtors.com Password: z#s%)F(913@2A Port: 21
        private static string poc_ftp_host = "qm20.siteground.biz";
        private static string poc_ftp_user = "Lsg@arubapalmsrealtors.com";
        private static string poc_ftp_pwd = "z#s%)F(913@2A";
        private static int poc_ftp_port = 21;

        public static bool DownloadDir()
        {
            //ServicePointManager.Expect100Continue = true;
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12; // | SecurityProtocolType.Ssl3;

            //string downloadFileLocation = "c:\\zunk";
            string ftpUrl = "ftp://" + poc_ftp_host + ":" + poc_ftp_port;

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Credentials = new NetworkCredential(poc_ftp_user, poc_ftp_pwd);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.UsePassive = true;
                var response = (FtpWebResponse)request.GetResponse();
                if (response == null)
                {
                    return false;
                }

                StreamReader streamReader = new StreamReader(response.GetResponseStream());

                List<string> directories = new List<string>();

                string line = streamReader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    var lineArr = line.Split('/');
                    line = lineArr[lineArr.Count() - 1];
                    directories.Add(line);
                    line = streamReader.ReadLine();
                    Console.WriteLine(line);
                }

                streamReader.Close();

                //to download file
                //request.Method = WebRequestMethods.Ftp.DownloadFile;
                //Stream responseStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(responseStream);
                //Console.WriteLine(reader.ReadToEnd());

                //Console.WriteLine("Download Complete, status {0}", response.StatusDescription);

                //reader.Close();

                response.Close();


                Logger.WriteInfo(moduleName, "DD", 0, "Ok");
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "DownloadDir", 0, ex);
                return false;
            }

            return false;

        }

        /// <summary>
        /// reads an input file, saves to database
        /// </summary>
        /// <param name="pgConStr">Connection string</param>
        /// <param name="pgSchema">Schema</param>
        /// <param name="inputFilePathName">input file full path + name</param>
        /// <param name="jsonParamFilePath">file path + name for file format definition json</param>
        /// to do: file details - in order to keep track of progress

        public static bool SaveInputToDB(string pgConStr, string pgSchema, int jobId, string inputFilePathName, string jsonParamFilePath, char theDelim)
        {
            Logger.Write(moduleName, "SaveInputToDB", 0, $"params: {jsonParamFilePath} file{inputFilePathName} with delimiter: {theDelim}", Logger.WARNING);

            bool saveOk = true;
            Dictionary<string, List<string>> fileDefDict = new();
            Dictionary<string, List<string>> jsonSkip = new();
            Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new();

            try
            {
                Util.LoadJsonParamFile(jsonParamFilePath, dbMap, jsonSkip, fileDefDict);

                int lineNo = 0;
                int startRowNo = 0;
                string line;
                int BufferSize = 4096;
                InputHeader inputHdr = new();
                InputRecord curRec = null;

                using (var fileStream = File.OpenRead(inputFilePathName))
                using (var sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNo++;
                        string[] cells = line.Split(theDelim);
                        if (cells.Length < SystemParam.RowTypeIndex + 1)
                        {
                            Logger.Write(moduleName, "SaveInputToDB", 0, $"Section Type not found - ignored line {lineNo} of file {inputFilePathName}", Logger.WARNING);
                            //to do : create error reporting
                            continue; // skip the line
                        }
                        ProcessDataRow(pgConStr, pgSchema, jobId, ref startRowNo, cells, inputHdr, ref curRec, ref saveOk, dbMap, jsonSkip, fileDefDict, lineNo, inputFilePathName);
                    }
                    if (curRec != null)
                    {
                        if (InsertCurrRec(pgConStr, pgSchema, jobId, startRowNo, lineNo, inputHdr, curRec) == false)
                            saveOk = false;
                        //to do save the current rec - last record
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "SaveInputToDB", 0, ex);
                return false;
            }

            return saveOk;
        }

        private static bool ProcessDataRow(string pgConStr, string pgSchema, int jobId, ref int startRowNo, string[] cells, InputHeader inputdr, ref InputRecord curRec, ref bool saveOk
                , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
                , Dictionary<string, List<string>> jsonSkip
                , Dictionary<string, List<string>> fileDefDict, int lineNo, string inputFile)
        {
            string rowType = cells[SystemParam.RowTypeIndex].ToUpper();

            if (rowType == SystemParam.FileHeaderRowType)
            {
                if (inputdr.HandleRow(fileDefDict[rowType], dbMap, null, rowType, cells) == false)
                {
                    Logger.Write(moduleName, "SaveInputToDB", 0, $"Failed to parse header line {lineNo} of file {inputFile}", Logger.ERROR);
                    //to do : create error reporting
                    saveOk = false;
                    return false; //no point reading file
                }
                else
                {
                    //to do Save header and get Id to link?
                }
                return true;
            }

            if (rowType == SystemParam.DataRowType)
            {
                if (curRec != null)
                {
                    if (InsertCurrRec(pgConStr, pgSchema, jobId, startRowNo, lineNo, inputdr, curRec) == false)
                    {
                        //to do : create error reporting
                        saveOk = false;
                    }
                }
                curRec = new();
                startRowNo = lineNo;
            }
            if (curRec == null)
            {
                Logger.Write(moduleName, "SaveInputToDB", 0, $"Failed to get started with Input Start Row. line {lineNo} of file {inputFile}", Logger.ERROR);
                //to do : create error reporting
                saveOk = false;
                return false; //no point reading file OR ignore startin rows ??
            }

            if (curRec.HandleRow(fileDefDict[rowType], dbMap, jsonSkip, rowType, cells) == false)
            {
                Logger.Write(moduleName, "SaveInputToDB", 0, $"Failed to parse data at line {lineNo} of file {inputFile}", Logger.ERROR);
                //to do : create error reporting
                saveOk = false;
            }

            return true;
        }

        private static void LoadJsonParamFile(string jsonParamFilePath
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            , Dictionary<string, List<string>> fileDefDict)
        {
            StreamReader sr = new(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            var oParams = JObject.Parse(fileAsStr);
            SetupSystemParams(oParams);
            LoadDbMapFromJson(oParams, dbMap);
            LoadSkipColumnsFromJson(oParams, jsonSkip);
            LoadInputFileDef(oParams, fileDefDict);
        }

        private static void LoadDbMapFromJson(JObject oParams, Dictionary<string, List<KeyValuePair<string, string>>> dbMap)
        {
            var paramSect = (JArray)oParams["database"];
            foreach (JObject chRowType in paramSect)
            {
                string rowType = ((string)chRowType["row_type"]).ToUpper();
                List<KeyValuePair<string, string>> dbColMap = new();

                foreach (JProperty jp in chRowType.Properties())
                {
                    if (jp.Name != "row_type")
                    {
                        dbColMap.Add(new KeyValuePair<string, string>(jp.Name, (string)jp.Value));
                    }
                }
                dbMap.Add(rowType, dbColMap);
            }
        }

        private static void LoadSkipColumnsFromJson(JObject oParams, Dictionary<string, List<string>> jsonSkip)
        {
            var paramSect = (JArray)oParams["json_skip"];

            foreach (JObject chRowType in paramSect)
            {
                string rowType = ((string)chRowType["row_type"]).ToUpper();
                JArray colsToSkipJA = (JArray)chRowType["cols"];
                List<string> columns = colsToSkipJA.ToObject<List<string>>();

                jsonSkip.Add(rowType, columns);
            }
        }

        private static void LoadInputFileDef(JObject oParams, Dictionary<string, List<string>> fileDefDict)
        {
            bool gotErr = false;
            JObject inputFileDef = (JObject)oParams["file_def"];
            var rowNodes = inputFileDef.Children().Children().ToList();
            foreach (JToken ch in rowNodes)
            {
                string rowType = ((JProperty)ch.Parent).Name.ToUpper(); //"FH" / "PD" / "CD"...
                Dictionary<int, string> keyValuePairs = new();
                List<int> colOrder = new();

                foreach (JProperty jp1 in ch.Children().ToList())
                {
                    if (int.TryParse(jp1.Name, out int k))
                    {
                        keyValuePairs.Add(k, (string)jp1.Value);
                        colOrder.Add(k);
                    }
                    else
                    {
                        gotErr = true;
                        Logger.Write(moduleName, "LoadInputFileDef", 0, $"Column order not int {jp1.Name} of {rowType}", Logger.ERROR);
                    }
                }

                if (gotErr)
                {
                    throw new Exception("invalid file def in row type " + rowType);
                }
                List<string> columns = new();
                foreach (var colOrd in colOrder.OrderBy(x => x).ToList())
                {
                    columns.Add(keyValuePairs[colOrd]);
                }

                fileDefDict.Add(rowType, columns);
            }
        }

        private static void SetupSystemParams(JObject oParams)
        {
            JObject sysParam = (JObject)oParams["system"];

            SystemParam.FileType = (string)sysParam["file_type"];
            SystemParam.RowTypeIndex = Convert.ToInt32(sysParam["index_of_row_type"]);
            SystemParam.FileHeaderRowType = ((string)sysParam["file_header_row_type"]).ToUpper();
            SystemParam.DataRowType = ((string)sysParam["data_row_type"]).ToUpper();
            SystemParam.DataTableName = ((string)sysParam["data_table_name"]).ToLower();
            SystemParam.DataTableJsonCol = ((string)sysParam["data_table_json_col"]).ToLower();
        }

        private static bool InsertCurrRec(string pgConStr, string pgSchema, int jobId, int startRowNo, int inputLineNo, InputHeader inputHdr, InputRecord curRec)
        {
            string insSql = curRec.GenerateInsert(pgSchema, SystemParam.DataTableName, SystemParam.DataTableJsonCol, jobId, startRowNo, inputHdr);
            try
            {
                return DbUtil.ExecuteNonSql(pgConStr, moduleName, jobId, inputLineNo, insSql);
            }
            catch 
            {
                return false;
            }
        }

        public static bool DownloadDirSFTP()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(poc_ftp_host, poc_ftp_port, poc_ftp_user, new PasswordAuthenticationMethod(poc_ftp_user, poc_ftp_pwd));
                using (var client = new SftpClient(connectionInfo))
                {
                    client.Connect();
                    var dirList = client.ListDirectory("");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "DownloadDir", 0, ex);
                return false;
            }
            return true;
        }

        public static Dictionary<string, List<string>> LoadJsonFromFile(string fullFilePath, int whatIf)
        {
            Dictionary<string, List<string>> fileDefDict = new();

            StreamReader sr = new(fullFilePath);
            string fileAsStr = sr.ReadToEnd();

            if (whatIf == 1)
            {
                Dictionary<string, Dictionary<string, string>> genJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileAsStr);

                foreach (string topSection in genJson.Keys)
                {
                    Dictionary<string, string> sectParams = genJson[topSection];
                    List<string> columns = new();

                    foreach (string chKey in sectParams.Keys.OrderBy(x => x))
                    {
                        columns.Add(sectParams[chKey]);
                    }

                    fileDefDict.Add(topSection, columns);
                }
            }
            else
            {
                var o = JObject.Parse(fileAsStr);
                var chNodes = o.Children();
                foreach (JProperty ch in chNodes)
                {
                    string x = ch.Name; //"fh" / "pd" / "cd"

                    Console.WriteLine(x);
                }

            }

            return fileDefDict;
        }

        public static void JsonTest()
        {
            Dictionary<string, Dictionary<string, string>> jDictPar = new();
            Dictionary<string, string> jDictCh = new();

            string k1 = "k1";
            string k2 = "k2";
            string v1 = "v1";
            string v2 = "v2";

            jDictCh.Add(k1, v1);
            jDictCh.Add(k2, v2);

            jDictPar.Add("firstOne", jDictCh);
            Dictionary<string, string> jDictCh2 = new();
            jDictCh2.Add("day1", "great");
            jDictCh2.Add("day2", "greater");

            jDictPar.Add("2ndToo", jDictCh2);

            string jStr = JsonConvert.SerializeObject(jDictPar);

            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}  {jStr}");

            jStr = "{\"k3\":\"v3\", \"k4\": \"Tom\", \"k5\": \"Jerry\"}";
            jDictCh = JsonConvert.DeserializeObject<Dictionary<string, string>>(jStr);
            foreach (var ke in jDictCh.Keys)
            {
                Console.WriteLine($"key: {ke} val: {jDictCh[ke]} ");
            }
        }

    }

    public class SystemParam
    {
        public static string FileType { get; set; }
        public static int RowTypeIndex { get; set; }
        public static string FileHeaderRowType { get; set; }
        public static string DataRowType { get; set; }
        public static string DataTableName { get; set; }
        public static string DataTableJsonCol { get; set; }
    }

    public abstract class InputRecordAbs
    {
        public List<KeyValuePair<string, string>> DbColsWithVals;

        public abstract bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
               , Dictionary<string, List<string>> jsonSkip
           , string rowType, string[] cells);

        internal void ParseDbValues(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict, string rowType, string[] cells)
        {
            List<KeyValuePair<string, string>> dbMap = null;
            if (dbMapDict.ContainsKey(rowType))
            {
                dbMap = dbMapDict[rowType];
            }
            if (dbMap != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    string aColHdr = allColumns[i];
                    AddDbColVal(dbMap, cells, i, aColHdr);
                }
            }
        }

        private void AddDbColVal(List<KeyValuePair<string, string>> dbMap, string[] cells, int i, string aColHdr)
        {
            foreach (var dbCol in dbMap)  //Note - no LINQ for KeyValuePair
            {
                if (dbCol.Key.ToUpper() == aColHdr.ToUpper())
                {
                    DbColsWithVals.Add(new KeyValuePair<string, string>(dbCol.Value, cells[i]));
                    break;
                }
            }
        }

    }

    public class InputHeader : InputRecordAbs
    {
        public List<KeyValuePair<string, string>> hdrFields = new();

        public override bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
                , Dictionary<string, List<string>> jsonSkip
            , string rowType, string[] cells)
        {
            if (allColumns == null || allColumns.Count != cells.Length)
                return false;

            for (int i = 0; i < allColumns.Count; i++)
            {
                hdrFields.Add(new KeyValuePair<string, string>(allColumns[i], cells[i]));
            }

            ParseDbValues(allColumns, dbMapDict, rowType, cells);
            return true;
        }
    }

    public class JsonColsWithVals
    {
        public List<KeyValuePair<string, string>> JsonColValPairs;
        public JsonColsWithVals()
        {
            JsonColValPairs = new();
        }

        internal void HandleRow(List<string> allColumns, List<string> skipJsonColumns, string[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                string aColHdr = allColumns[i];
                bool skipJ = (skipJsonColumns != null && skipJsonColumns.Contains(aColHdr));
                if (skipJ == false)
                {
                    JsonColValPairs.Add(new KeyValuePair<string, string>(aColHdr, cells[i]));
                }
            }
        }

        internal void RenderJson(StringBuilder sb2)
        {
            bool first = true;
            sb2.Append('{');

            foreach (var jPair in JsonColValPairs)
            {
                if (first == false)
                {
                    sb2.Append(',');
                }

                sb2.Append('"').Append(jPair.Key).Append("\":\"").Append(jPair.Value).Append('"');
                first = false;
            }

            sb2.Append('}');
        }
    }
    public class JsonArrOfColsWithVals
    {
        public List<JsonColsWithVals> JsonColsWithVals;

        public JsonArrOfColsWithVals()
        {
            JsonColsWithVals = new();
        }

        internal void HandleRow(List<string> allColumns, List<string> skipJsonColumns, string[] cells)
        {
            JsonColsWithVals jColVals = new();

            jColVals.HandleRow(allColumns, skipJsonColumns, cells);

            JsonColsWithVals.Add(jColVals);
        }

        internal void RenderJson(StringBuilder sb2)
        {
            sb2.Append('[');
            bool first = true;

            foreach (var jColVal in JsonColsWithVals)
            {
                if (first == false)
                {
                    sb2.Append(',');
                }

                jColVal.RenderJson(sb2);
                first = false;
            }
            sb2.Append(']');
        }
    }

    public class InputRecord : InputRecordAbs
    {
        public Dictionary<string, JsonArrOfColsWithVals> JsonByRowType;

        public InputRecord()
        {
            DbColsWithVals = new();
            JsonByRowType = new();
        }

        public override bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
                , Dictionary<string, List<string>> jsonSkip
            , string rowType, string[] cells)
        {
            if (allColumns == null || allColumns.Count != cells.Length)
                return false;

            JsonArrOfColsWithVals rowsOfGivenRowType = null;
            if (JsonByRowType.ContainsKey(rowType))
            {
                rowsOfGivenRowType = JsonByRowType[rowType];
            }
            else
            {
                rowsOfGivenRowType = new();
            }

            List<string> skipJsonColumns = null;
            if (jsonSkip.ContainsKey(rowType))
            {
                skipJsonColumns = jsonSkip[rowType];
            }

            rowsOfGivenRowType.HandleRow(allColumns, skipJsonColumns, cells);

            if (JsonByRowType.ContainsKey(rowType) == false)
            {
                JsonByRowType.Add(rowType, rowsOfGivenRowType);
            }
            //else
            //    JsonByRowType[rowType] = rowsOfGivenRowType;  //not needed 

            ParseDbValues(allColumns, dbMapDict, rowType, cells);

            return true;
        }

        public string GenerateInsert(string pgSchema, string dataTableName, string jsonColName, int jobId, int startRowNo, InputHeader inputHdr)
        {
            StringBuilder sb1 = new();
            StringBuilder sb2 = new();
            sb1.Append("insert into ").Append(pgSchema).Append('.').Append(dataTableName);
            sb1.Append("(job_id, row_number");
            sb2.Append(jobId).Append(',').Append(startRowNo);

            AppendDbMap(sb1, sb2, inputHdr.DbColsWithVals);
            AppendDbMap(sb1, sb2, DbColsWithVals);
            sb1.Append(',');
            sb2.Append(',');
            sb1.Append(jsonColName);
            sb2.Append('\'');

            //string jStr = JsonConvert.SerializeObject(JsonByRowType);
            bool first = true;
            StringBuilder jStr = new();
            jStr.Append('{');
            foreach (var jsonRow in JsonByRowType)
            {
                if (first == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(jsonRow.Key).Append("\":");
                jsonRow.Value.RenderJson(jStr);
                first = false;
            }
            jStr.Append('}');

            sb2.Append(jStr.Replace("'", "''"));
            sb2.Append('\'');
            sb1.Append(") values (").Append(sb2).Append(')');

            return sb1.ToString();
        }

        private void AppendDbMap(StringBuilder sb1, StringBuilder sb2, List<KeyValuePair<string, string>> aDbColsWithVals)
        {
            foreach (var dbColVal in aDbColsWithVals)
            {
                sb1.Append(',');
                sb2.Append(',');

                sb1.Append(dbColVal.Key);
                sb2.Append('\'').Append(dbColVal.Value.Replace("'", "''")).Append('\'');

            }
        }
    }

}
