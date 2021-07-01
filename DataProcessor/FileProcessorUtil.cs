using CommonUtil;
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
        public static bool SaveInputToDB(FileProcessor fileProcessor, int jobId, string inputFilePathName, string jsonParamFilePath
            , Dictionary<string, string> paramsDict, string dateAsDir)
        {
            Logger.Write(logProgName, "SaveInputToDB", 0, $"params: {jsonParamFilePath} file{inputFilePathName} ", Logger.WARNING);

            bool saveOk = true;
            Dictionary<string, List<string>> fileDefDict = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> jsonSkip = new Dictionary<string, List<string>>();
            Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new Dictionary<string, List<KeyValuePair<string, string>>>();
            SaveAsFileDef saveAsFileDefnn = new SaveAsFileDef();
            SystemParamInput inpSysParam = new SystemParamInput();

            try
            {
                LoadJsonParamFile(jsonParamFilePath, dbMap, jsonSkip, fileDefDict, saveAsFileDefnn, inpSysParam);
                char theDelim = inpSysParam.Delimt;

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
                        ProcessDataRow(fileProcessor, jobId, ref startRowNo, cells, inputHdr
                            , ref curRec, ref saveOk, dbMap, jsonSkip, fileDefDict, lineNo, inputFilePathName
                            , saveAsFileDefnn, inpSysParam, paramsDict, dateAsDir);
                    }
                    if (curRec != null)
                    {
                        //to do REMOVE harcoded output_lite 

                        if (InsertCurrRec(fileProcessor, jobId, inpSysParam, startRowNo, lineNo
                            , inputHdr, curRec, paramsDict, dateAsDir) == false)
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

        private static bool ProcessDataRow(FileProcessor fileProcessor, int jobId, ref int startRowNo, string[] cells, InputHeader inputHdr
            , ref InputRecord curRec, ref bool saveOk
            , Dictionary<string, List<KeyValuePair<string, string>>> dbMap
            , Dictionary<string, List<string>> jsonSkip
            , Dictionary<string, List<string>> fileDefDict, int lineNo, string inputFile
            , SaveAsFileDef saveAsFileDefnn
            , SystemParamInput inpSysParam
            , Dictionary<string, string> paramsDict, string dateAsDir)
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
                    //to do REMOVE harcoded output_lite 

                    if (InsertCurrRec(fileProcessor, jobId, inpSysParam, startRowNo, lineNo
                        , inputHdr, curRec, paramsDict, dateAsDir) == false)
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

        private static bool InsertCurrRec(FileProcessor fileProcessor, int jobId, SystemParamInput inpSysParam, int startRowNo
            , int inputLineNo, InputHeader inputHdr, InputRecord curRec
            , Dictionary<string, string> paramsDict, string dateAsDir)
        {
            string pgConnection = fileProcessor.GetConnection();
            string pgSchema = fileProcessor.GetSchema();

            string selSql = curRec.GenerateRecFind(pgSchema, inpSysParam);
            if (DbUtil.IsRecFound(pgConnection, logProgName, fileProcessor.GetModuleName(), jobId, startRowNo, selSql, true, out int id))
            {
                //to do mark dup ??
                Logger.Write(logProgName, "InsertCurrRec", 0, $"ignored row {startRowNo} duplicate rec Was saved as id:{id}", Logger.ERROR);
                return false;
            }

            string courr = curRec.GetColumnValue(inpSysParam.CourierCol);

            bool filesOk = WriteImageFiles(pgConnection, pgSchema, fileProcessor, jobId, paramsDict, startRowNo, curRec, dateAsDir, courr, inpSysParam);
            if (filesOk == false)
            {
                Logger.Write(logProgName, "InsertCurrRec", 0, $"Skipped insert- could not create files {fileProcessor.GetModuleName()} : {jobId} : rows={startRowNo}-{inputLineNo} : date={dateAsDir} ", Logger.ERROR);
                //to do error handling
                return false; //---------------- No more processing
            }
            
            //to do populate fileJSONB - new column for saving {"photo" : "c:\...\photo\...jpg", "sign": "c:\...\sign...jpg" }

            string insSql = curRec.GenerateInsert(pgSchema, inpSysParam.DataTableName, inpSysParam.DataTableJsonCol, jobId, startRowNo, inputHdr);
            try
            {
                DbUtil.ExecuteNonSql(pgConnection, logProgName, fileProcessor.GetModuleName(), jobId, inputLineNo, insSql);
                return true;
            }
            catch
            {
                return false;  //---------------- No more processing
            }
        }

        private static bool WriteImageFiles(string pgConnection, string pgSchema, FileProcessor fileProcessor, int jobId, Dictionary<string, string> paramsDict, int startRowNo, InputRecord curRec
            , string dateAsDir, string courierSName, SystemParamInput inpSysParam)
        {
            try
            {
                string bizDir = fileProcessor.GetBizTypeDirName(curRec);
                string curKey = curRec.GetColumnValue(inpSysParam.UniqueColumn);
                string courierSeq = SequenceGen.GetCourierSeq(pgConnection, pgSchema, courierSName, 5);                 //courier_seq - same seq # for all files

                int maxFilesPerSub = 150;
                int maxDirExpexcted = 9999;
                int.TryParse(paramsDict[ConstantBag.PARAM_IMAGE_LIMIT], out maxFilesPerSub);
                int.TryParse(paramsDict[ConstantBag.PARAM_SUBDIR_APROX_LIMIT], out maxDirExpexcted);

                foreach (var fileToWrite in curRec.saveAsFiles)
                {
                    string derivedFN = fileToWrite.FileName.Replace(ConstantBag.FILE_NAME_TAG_UNIQUE_COL, curKey);
                    derivedFN = derivedFN.Replace(ConstantBag.FILE_NAME_TAG_COUR_SEQ, courierSeq);

                    string fullFilePath = paramsDict[ConstantBag.PARAM_WORK_DIR]
                        + "\\" + dateAsDir  // "yyyymmdd" 
                        + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
                        + "\\" + bizDir // module + bizType based dir = ("output_apy or output_lite") ---- hard coded in caller
                        + "\\" + courierSName + "_" + dateAsDir
                        + "\\" + fileToWrite.Dir;  //Photo | Sign

                    string subDirWithNum = SequenceGen.GetFileDirWithSeq(fullFilePath
                        , fileToWrite.SubDir  //sig_ or Photo_
                        , maxFilesPerSub  //150 max per each sub dir
                        , maxDirExpexcted //9999 will generate patter of 0001..9999
                        );

                    fullFilePath = fullFilePath
                        // {{date_dir_seq}} get sequence ("lite",  dateAsDir, Dir, paramsDict[bizTypeKey] with 150 limit
                        + "\\" + fileToWrite.SubDir; //sig_001 

                    if (Directory.Exists(fullFilePath) == false)
                        Directory.CreateDirectory(fullFilePath);

                    string fullFilePathNm = fullFilePath + "\\" + derivedFN;  //{{courier_seq}}_{{pran_id}}_sign.jpg

                    string hex = fileToWrite.FileContent;
                    
                    var bytes = Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();

                    File.WriteAllBytes(fullFilePathNm, bytes);

                    if (File.Exists(fullFilePathNm))
                    {
                        Logger.Write(logProgName, "WriteFiles", jobId, fileProcessor.GetModuleName() + ", row:" + startRowNo + " deleting " + fileToWrite.Dir + ", full:" + fullFilePathNm, Logger.WARNING);
                        File.Delete(fullFilePathNm);
                    }
                    fileToWrite.PhysicalPath = fullFilePathNm;
                }
            }
            catch (Exception ex)
            {
                Logger.Write(logProgName, "WriteFiles", jobId, fileProcessor.GetModuleName() + ", row:" + startRowNo + " courier:" + courierSName + " see error below", Logger.ERROR);
                Logger.WriteEx(logProgName, "WriteFiles", jobId, ex);
                return false;
            }
            return true;
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
            JObject sysParamSect = (JObject)oParams[ConstantBag.FD_SYSTEM_PARAM];

            inpSysParam.FileType = (string)sysParamSect[ConstantBag.FD_FILE_TYPE];
            inpSysParam.Delimt = (char)sysParamSect[ConstantBag.FD_DELIMT];
            inpSysParam.RowTypeIndex = Convert.ToInt32(sysParamSect[ConstantBag.FD_INDEX_OF_ROW_TYPE]);
            inpSysParam.FileHeaderRowType = ((string)sysParamSect[ConstantBag.FD_FILE_HEADER_ROW_TYPE]).ToUpper();

            inpSysParam.DataRowType = ((string)sysParamSect[ConstantBag.FD_DATA_ROW_TYPE]).ToUpper();
            inpSysParam.DataTableName = ((string)sysParamSect[ConstantBag.FD_DATA_TABLE_NAME]).ToLower();
            inpSysParam.DataTableJsonCol = ((string)sysParamSect[ConstantBag.FD_DATA_TABLE_JSON_COL]).ToLower();
            inpSysParam.UniqueColumn = ((string)sysParamSect[ConstantBag.FD_UNIQUE_COLUMN]).ToLower();
            inpSysParam.CourierCol = ((string)sysParamSect[ConstantBag.FD_COURIER_COL]).ToLower();
        }

        #endregion
    }
}
