using DbOps;
using DbOps.Structs;
using Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataProcessor
{
    public class FileProcessorUtil
    {
        private const string logProgName = "FileProcessorUtil";
        public static bool SaveInputToDB(string pgConnection, string pgSchema, string moduleName, int jobId, string inputFilePathName, string jsonParamFilePath, char theDelim)
        {
            Logger.Write(logProgName, "SaveInputToDB", 0, $"params: {jsonParamFilePath} file{inputFilePathName} with delimiter: {theDelim}", Logger.WARNING);

            bool saveOk = true;
            Dictionary<string, List<string>> fileDefDict = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> jsonSkip = new Dictionary<string, List<string>>();
            //Dictionary<string, List<string>> saveAsFiles = new Dictionary<string, List<string>>(); //rec type : column etc --to do
            Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new Dictionary<string, List<KeyValuePair<string, string>>>();

            try
            {
                LoadJsonParamFile(jsonParamFilePath, dbMap, jsonSkip, fileDefDict);

                int lineNo = 0;
                int startRowNo = 0;
                string line;
                int BufferSize = 4096;
                InputHeader inputHdr = new InputHeader();
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
                            Logger.Write(logProgName, "SaveInputToDB", 0, $"Section Type not found - ignored line {lineNo} of file {inputFilePathName}", Logger.WARNING);
                            //to do : create error reporting
                            continue; // skip the line
                        }
                        ProcessDataRow(pgConnection, pgSchema, moduleName, jobId, ref startRowNo, cells, inputHdr
                            , ref curRec, ref saveOk, dbMap, jsonSkip, //saveAsFiles,
                            fileDefDict, lineNo, inputFilePathName);
                    }
                    if (curRec != null)
                    {
                        if (InsertCurrRec(pgConnection, pgSchema, moduleName, jobId, startRowNo, lineNo, inputHdr, curRec) == false)
                            saveOk = false;
                        //to do save the current rec - last record
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgName, "SaveInputToDB", 0, ex);
                return false;
            }

            return saveOk;
        }

        private static bool ProcessDataRow(string pgConnection, string pgSchema, string moduleName, int jobId, ref int startRowNo, string[] cells, InputHeader inputdr
            , ref InputRecord curRec, ref bool saveOk
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            //, saveAsFiles
            , Dictionary<string, List<string>> fileDefDict, int lineNo, string inputFile)
        {
            string rowType = cells[SystemParam.RowTypeIndex].ToUpper();

            if (rowType == SystemParam.FileHeaderRowType)
            {
                if (inputdr.HandleRow(fileDefDict[rowType], dbMap, null, rowType, cells) == false)
                {
                    Logger.Write(logProgName, "SaveInputToDB", 0, $"Failed to parse header line {lineNo} of file {inputFile}", Logger.ERROR);
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
                    if (InsertCurrRec(pgConnection, pgSchema, moduleName, jobId, startRowNo, lineNo, inputdr, curRec) == false)
                    {
                        //to do : create error reporting
                        saveOk = false;
                    }
                }
                curRec = new InputRecord();
                startRowNo = lineNo;
            }
            if (curRec == null)
            {
                Logger.Write(logProgName, "SaveInputToDB", 0, $"Failed to get started with Input Start Row. line {lineNo} of file {inputFile}", Logger.ERROR);
                //to do : create error reporting
                saveOk = false;
                return false; //no point reading file OR ignore startin rows ??
            }

            if (curRec.HandleRow(fileDefDict[rowType], dbMap, jsonSkip, rowType, cells) == false)
            {
                Logger.Write(logProgName, "SaveInputToDB", 0, $"Failed to parse data at line {lineNo} of file {inputFile}", Logger.ERROR);
                //to do : create error reporting
                saveOk = false;
            }

            return true;
        }

        private static bool InsertCurrRec(string pgConnection, string pgSchema, string moduleName, int jobId, int startRowNo, int inputLineNo, InputHeader inputHdr, InputRecord curRec)
        {
            string insSql = curRec.GenerateInsert(pgSchema, SystemParam.DataTableName, SystemParam.DataTableJsonCol, jobId, startRowNo, inputHdr);
            try
            {
                return DbUtil.ExecuteNonSql(pgConnection, logProgName, moduleName, jobId, inputLineNo, insSql);
            }
            catch
            {
                return false;
            }
        }

        #region LoadParams

        private static void LoadJsonParamFile(string jsonParamFilePath
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            , Dictionary<string, List<string>> fileDefDict)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
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
                List<KeyValuePair<string, string>> dbColMap = new List<KeyValuePair<string, string>>();

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
                Dictionary<int, string> keyValuePairs = new Dictionary<int, string>();
                List<int> colOrder = new List<int>();

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
                        Logger.Write(logProgName, "LoadInputFileDef", 0, $"Column order not int {jp1.Name} of {rowType}", Logger.ERROR);
                    }
                }

                if (gotErr)
                {
                    throw new Exception("invalid file def in row type " + rowType);
                }
                List<string> columns = new List<string>();
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

        #endregion
    }
}
