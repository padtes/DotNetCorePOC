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
            Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new Dictionary<string, List<KeyValuePair<string, string>>>();
            SaveAsFileDef saveAsFileDefnn = new SaveAsFileDef();
            SystemParamInput inpSysParam = new SystemParamInput();

            try
            {
                LoadJsonParamFile(jsonParamFilePath, dbMap, jsonSkip, fileDefDict, saveAsFileDefnn, inpSysParam);

                int lineNo = 0;
                int startRowNo = 0;
                string line;
                int BufferSize = 4096;
                InputHeader inputHdr = new InputHeader();
                InputRecord curRec = null;
                string errline = "";

                using (var fileStream = File.OpenRead(inputFilePathName))
                using (var sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        CheckPrevLineError(inputFilePathName, lineNo, ref errline);

                        lineNo++;
                        string[] cells = line.Split(theDelim);
                        if (cells.Length < inpSysParam.RowTypeIndex + 1)
                        {
                            errline = line;
                            continue; // skip the line
                        }
                        ProcessDataRow(pgConnection, pgSchema, moduleName, jobId, ref startRowNo, cells, inputHdr
                            , ref curRec, ref saveOk, dbMap, jsonSkip, fileDefDict, lineNo, inputFilePathName, saveAsFileDefnn, inpSysParam);
                    }
                    if (curRec != null)
                    {
                        if (InsertCurrRec(pgConnection, pgSchema, moduleName, jobId, inpSysParam, startRowNo, lineNo, inputHdr, curRec) == false)
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

        private static void CheckPrevLineError(string inputFilePathName, int lineNo, ref string errline)
        {
            if (errline != "")  //this is done to report error only if it is not the last line of file - which is checksum line
            {
                Logger.Write(logProgName, "SaveInputToDB", 0, $"Row Type not found - see below ignored line {lineNo} of file {inputFilePathName}", Logger.WARNING);
                Logger.Write(logProgName, "SaveInputToDB", 0, "err line:[" + errline + "]", Logger.WARNING);
                //to do : create error reporting
                errline = "";
            }
        }

        private static bool ProcessDataRow(string pgConnection, string pgSchema, string moduleName, int jobId, ref int startRowNo, string[] cells, InputHeader inputHdr
            , ref InputRecord curRec, ref bool saveOk
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            , Dictionary<string, List<string>> fileDefDict, int lineNo, string inputFile
            , SaveAsFileDef saveAsFileDefnn
            , SystemParamInput inpSysParam)
        {
            string rowType = cells[inpSysParam.RowTypeIndex].ToUpper();

            if (rowType == inpSysParam.FileHeaderRowType)
            {
                if (inputHdr.HandleRow(fileDefDict[rowType], dbMap, null, null, rowType, cells) == false)
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

            if (rowType == inpSysParam.DataRowType)
            {
                if (curRec != null)
                {
                    if (InsertCurrRec(pgConnection, pgSchema, moduleName, jobId, inpSysParam, startRowNo, lineNo, inputHdr, curRec) == false)
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

            if (curRec.HandleRow(fileDefDict[rowType], dbMap, jsonSkip, saveAsFileDefnn, rowType, cells) == false)
            {
                Logger.Write(logProgName, "SaveInputToDB", 0, $"Failed to parse data at line {lineNo} of file {inputFile}", Logger.ERROR);
                //to do : create error reporting
                saveOk = false;
            }

            return true;
        }

        private static bool InsertCurrRec(string pgConnection, string pgSchema, string moduleName, int jobId, SystemParamInput inpSysParam, int startRowNo
            , int inputLineNo, InputHeader inputHdr, InputRecord curRec)
        {
            string selSql = curRec.GenerateRecFind(pgSchema, inpSysParam);
            if (DbUtil.IsRecFound(pgConnection, moduleName, logProgName, jobId, startRowNo, selSql, true, out int id))
            {
                //to do mark dup ??
                Logger.Write(logProgName, "InsertCurrRec", 0, $"ignored row {startRowNo} duplicate rec Was saved as id:{id}", Logger.ERROR);
                return false;
            }

            string insSql = curRec.GenerateInsert(pgSchema, inpSysParam.DataTableName, inpSysParam.DataTableJsonCol, jobId, startRowNo, inputHdr);
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

        public static void LoadJsonParamFile(string jsonParamFilePath
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            , Dictionary<string, List<string>> fileDefDict
            , SaveAsFileDef saveAsFileDefnn
            , SystemParamInput inpSysParam)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            var oParams = JObject.Parse(fileAsStr);
            SetupSystemParams(oParams, inpSysParam);
            LoadDbMapFromJson(oParams, dbMap);
            LoadSkipColumnsFromJson(oParams, jsonSkip);
            LoadInputFileDef(oParams, fileDefDict);
            LoadSaveAsFileDef(oParams, saveAsFileDefnn);
        }

        private static void LoadSaveAsFileDef(JObject oParams, SaveAsFileDef saveAsFileDefnn)
        {
            var paramSect = (JArray)oParams["save_as_file"];
            if (paramSect != null)
            {
                foreach (JObject chRowType in paramSect)
                {
                    string rowType = ((string)chRowType["row_type"]).ToUpper();
                    JArray colsToSkipJA = (JArray)chRowType["columns"];
                    List<SaveAsFileColumn> tmpColumns = colsToSkipJA.ToObject<List<SaveAsFileColumn>>();

                    saveAsFileDefnn.SaveAsFile.Add(new SaveAsFile() { RowType = rowType, Columns = tmpColumns });
                }
            }
        }

        private static void LoadDbMapFromJson(JObject oParams, Dictionary<string, List<KeyValuePair<string, string>>> dbMap)
        {
            var paramSect = (JArray)oParams["database"];
            if (paramSect != null)
            {
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
        }

        private static void LoadSkipColumnsFromJson(JObject oParams, Dictionary<string, List<string>> jsonSkip)
        {
            var paramSect = (JArray)oParams["json_skip"];
            if (paramSect != null)
            {
                foreach (JObject chRowType in paramSect)
                {
                    string rowType = ((string)chRowType["row_type"]).ToUpper();
                    JArray colsToSkipJA = (JArray)chRowType["cols"];
                    List<string> columns = colsToSkipJA.ToObject<List<string>>();

                    jsonSkip.Add(rowType, columns);
                }
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

        private static void SetupSystemParams(JObject oParams, SystemParamInput inpSysParam)
        {
            JObject sysParamSect = (JObject)oParams["system"];

            inpSysParam.FileType = (string)sysParamSect["file_type"];
            inpSysParam.RowTypeIndex = Convert.ToInt32(sysParamSect["index_of_row_type"]);
            inpSysParam.FileHeaderRowType = ((string)sysParamSect["file_header_row_type"]).ToUpper();
            inpSysParam.DataRowType = ((string)sysParamSect["data_row_type"]).ToUpper();
            inpSysParam.DataTableName = ((string)sysParamSect["data_table_name"]).ToLower();
            inpSysParam.DataTableJsonCol = ((string)sysParamSect["data_table_json_col"]).ToLower();
            inpSysParam.UniqueColumn= ((string)sysParamSect["unique_column"]).ToLower();
        }

        #endregion
    }
}
