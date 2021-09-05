using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using Newtonsoft.Json;
using NpsScriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class InputRecord : InputRecordAbs
    {
        private const string FILE_SAVE_PLACEHOLDER = "###FILESAVE###";
        private Dictionary<string, string> tmpCourrAWB = new Dictionary<string, string>();

        public Dictionary<string, JsonArrOfColsWithVals> JsonByRowType = new Dictionary<string, JsonArrOfColsWithVals>();
        public List<SaveAsFileColumn> saveAsFiles = new List<SaveAsFileColumn>();
        public List<SequenceColWithVal> sequenceCols = new List<SequenceColWithVal>();
        public List<MappedColWithVal> mappedCols = new List<MappedColWithVal>();
        public Dictionary<string, string> allDerivedColVal = new Dictionary<string, string>();

        public override bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
            , Dictionary<string, List<string>> jsonSkip, SaveAsFileDef saveAsFileDefnn
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
                rowsOfGivenRowType = new JsonArrOfColsWithVals();
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

            ParseSaveAsFileValues(allColumns, saveAsFileDefnn, rowType, cells);

            return true;
        }

        public string GenerateRecFind(string pgSchema, SystemParamInput inpSysParam)
        {
            string valToCheck = GetColumnValue(inpSysParam.UniqueColumn);
            valToCheck = valToCheck.Replace("'", "''");

            String sql = $"select id from {pgSchema}.{inpSysParam.DataTableName} where {inpSysParam.UniqueColumn} = '{valToCheck}'";
            return sql;
        }

        public string GenerateInsert(string pgConnection, string pgSchema, string logProgName, string moduleName
            , string sysPath, JsonInputFileDef jDef, int jobId, int startRowNo, int fileinfoId, string inputFile, InputHeader inputHdr)
        {
            string dataTableName = jDef.inpSysParam.DataTableName;
            string jsonColName = jDef.inpSysParam.DataTableJsonCol;

            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb1.Append("insert into ").Append(pgSchema).Append('.').Append(dataTableName);
            sb1.Append("(fileinfo_id, row_number, files_saved");
            sb2.Append(fileinfoId).Append(',').Append(startRowNo).Append(',').Append(FILE_SAVE_PLACEHOLDER);

            AppendDbMap(sb1, sb2, inputHdr.DbColsWithVals);
            AppendDbMap(sb1, sb2, DbColsWithVals);
            sb1.Append(',');
            sb2.Append(',');
            sb1.Append(jsonColName);
            sb2.Append('\'');

            //string jStr = JsonConvert.SerializeObject(JsonByRowType);
            bool first = true;
            StringBuilder jStr = new StringBuilder();

            first = BuildJsonForInputRows(inputFile, inputHdr, first, jStr);

            GenerateMappedColumnPart(sysPath, jDef, inputHdr, first, jStr, false, out _);
            //done mapped columns
            jStr.Append('}');

            sb2.Append(jStr.Replace("'", "''"));
            sb2.Append('\'');
            sb1.Append(") values (").Append(sb2).Append(')');

            return sb1.ToString();
        }

        private bool BuildJsonForInputRows(string inputFile, InputHeader inputHdr, bool first, StringBuilder jStr)
        {
            jStr.Append('{');

            AppendFHtoDet(inputHdr, jStr, inputFile);

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

            return first;
        }

        private static void AppendFHtoDet(InputHeader inputHdr, StringBuilder jStr, string inputFile)
        {
            FileInfo fi = new FileInfo(inputFile);
            jStr.Append("\"fh\":{\"xinpfile\":\"" + fi.Name + '"');
            foreach (KeyValuePair<string, string> hdr in inputHdr.hdrFields)
            {
                jStr.Append(',').Append('"')
                    .Append(hdr.Key)
                    .Append("\":\"")
                    .Append(JsonColsWithVals.EscapeJson(hdr.Value))
                    .Append('"');
            }
            jStr.Append("},");
        }

        private void GenerateMappedColumnPart(string sysPath, JsonInputFileDef jDef, InputHeader inputHdr, bool hasNoOtherRows, StringBuilder jStr
            , bool justPreScriban, out string almostWholeJson)
        {
            if (hasNoOtherRows == false)
            {
                jStr.Append(',');
            }
            jStr.Append('"').Append("xx").Append("\":"); //extended mapped and scripted values
            jStr.Append('{');
            bool first = true;

            for (int i = 0; i < mappedCols.Count; i++)
            {
                if (first == false)
                {
                    jStr.Append(',');
                }

                AddToJsonFromMapped(jStr, mappedCols[i]);
                first = false;
            }

            if (justPreScriban == false)
            {
                AddToJsonFromSequences(jStr, ref first);
            }

            almostWholeJson = jStr.ToString()
                + "}}";

            if (justPreScriban == false)
            {
                AddToJsonFromScriban("inputRecord", jStr, first, sysPath, jDef, almostWholeJson);

                jStr.Append('}'); //end "xx"

                almostWholeJson = ""; // clear out
            }
        }

        //private string AppendFileHeader(InputHeader inputHdr)
        //{
        //    char qt = '\"';
        //    String stRet = $", {qt}fh{qt}:{{";
        //    bool first = true;
        //    foreach (KeyValuePair<string, string> item in inputHdr.hdrFields)
        //    {
        //        if (first == false)
        //            stRet += ",";
        //        stRet += $"{qt}{item.Key}{qt}:{qt}{item.Value}{qt}";
        //        first = false;
        //    }
        //    stRet += "}";
        //    return stRet;
        //}

        private void AddToJsonFromSequences(StringBuilder jStr, ref bool hasNoOtherRows)
        {
            foreach (SequenceColWithVal seqCol in this.sequenceCols)
            {
                if (hasNoOtherRows == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(seqCol.DestCol).Append("\":");
                jStr.Append('"').Append(JsonColsWithVals.EscapeJson(seqCol.SequenceStr)).Append("\"");
                hasNoOtherRows = false;
            }
        }

        private void AddToJsonFromScriban(string logProgName, StringBuilder jStr, bool hasNoOtherRows, string sysPath, JsonInputFileDef jDef, string almostWholeJson)
        {
            bool first = hasNoOtherRows;
            foreach (ScriptCol scrptCol in jDef.scrpitedColDefnn.ScriptColList)
            {
                string val = "";
                try
                {
                    val = ScribanHandler.Generate(sysPath, scrptCol, almostWholeJson, false, false);
                }
                catch (Exception ex)
                {
                    if (ScribanHandler.IsSameError(ex.Message) == false)
                        Logger.WriteEx(logProgName, "AddToJsonFromScriban", 0, ex);
                }
                if (first == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(scrptCol.DestCol).Append("\":");
                jStr.Append('"').Append(JsonColsWithVals.EscapeJson(val)).Append("\"");
                first = false;

                if (allDerivedColVal.ContainsKey(scrptCol.DestCol) == false)
                    allDerivedColVal.Add(scrptCol.DestCol, val);
            }
        }

        internal void PrepareColumns(bool withLock, string pgConnection, string pgSchema, string logProgName, string moduleName, JsonInputFileDef jDef
            , int jobId, int startRowNo, string runFor
            , string sysPath, string inputFile, InputHeader inputHdr)
        {
            string cardType = ConstantBag.CARD_NA;

            foreach (var item in DbColsWithVals)
            {
                if (item.Key == ConstantBag.APY_FLAG_DB_COL_NAME)
                {
                    if (item.Value == "1" || item.Value.ToUpper() == "Y")
                        cardType = ConstantBag.CARD_APY;
                    else
                        cardType = ConstantBag.CARD_LITE;
                }
            }

            GetMappedColValues(pgConnection, pgSchema, logProgName, moduleName, jDef, jobId, startRowNo);

            //pre eval phase
            Dictionary<string, string> evalCols = new Dictionary<string, string>();
            EvalPreSeq(logProgName, jDef, sysPath, inputFile, inputHdr, evalCols);

            GetSequenceValues(withLock, pgConnection, pgSchema, logProgName, moduleName, jobId, jDef, evalCols, runFor, cardType);
        }

        private void EvalPreSeq(string logProgName, JsonInputFileDef jDef, string sysPath, string inputFile, InputHeader inputHdr, Dictionary<string, string> evalCols)
        {
            StringBuilder jStr = new StringBuilder();
            bool first = BuildJsonForInputRows(inputFile, inputHdr, true, jStr);
            string almostWholeJson;
            GenerateMappedColumnPart(sysPath, jDef, inputHdr, first, jStr, true, out almostWholeJson);

            //List<string> scribanToEval = new List<string>();
            //scribanToEval.Add("x_pst_type");

            foreach (string aColName in jDef.scribanToEval)
            {
                foreach (ScriptCol scrptCol in jDef.scrpitedColDefnn.ScriptColList)
                {
                    if (scrptCol.DestCol == aColName)
                    {
                        string val = "";
                        try
                        {
                            val = ScribanHandler.Generate(sysPath, scrptCol, almostWholeJson, false, false);
                            if (evalCols.ContainsKey(aColName))
                            {
                                evalCols[aColName] = val;
                            }
                            else
                            {
                                evalCols.Add(aColName, val);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ScribanHandler.IsSameError(ex.Message) == false)
                                Logger.WriteEx(logProgName, "EvalPreSeq", 0, ex);
                        }
                        break;
                    }
                }
            }
        }

        private static void AddToJsonFromMapped(StringBuilder jStr, MappedColWithVal col)
        {
            jStr.Append('"').Append(col.theDef.DestCol).Append("\":");
            jStr.Append('"').Append(JsonColsWithVals.EscapeJson(col.mappedResult)).Append("\"");
        }

        public bool GetInputVal(string rowType, int indOfRow, string sourceCol, out string srcVal)
        {
            srcVal = "";
            if (JsonByRowType.ContainsKey(rowType) == false)
            {
                return false;
            }
            JsonArrOfColsWithVals row = JsonByRowType[rowType];
            if (row.JsonColsWithVals == null || row.JsonColsWithVals.Count < indOfRow + 1)
            {
                return false;
            }
            JsonColsWithVals jColVal = row.JsonColsWithVals[indOfRow];

            foreach (KeyValuePair<string, string> kv in jColVal.JsonColValPairs)
            {
                if (kv.Key == sourceCol)
                {
                    srcVal = kv.Value;
                    return true;
                }
            }
            return false;
        }

        public string ReplaceFileSaveJsonSql(string insSql, int startRowNo)
        {
            string jsonSer;
            //get json if files were saved
            StringBuilder sbFS = new StringBuilder("'");

            bool hasSaved = false;
            foreach (SaveAsFileColumn fileToWrite in this.saveAsFiles)
            {
                if (fileToWrite.PhysicalPath != "")
                {
                    hasSaved = true;
                    break;
                }
            }
            if (hasSaved)
            {
                if (this.saveAsFiles.Count == 2)  //make sure phot is always 1st, sign second
                {
                    var tmp1 = this.saveAsFiles[0];
                    var tmp2 = this.saveAsFiles[1];
                    if (tmp1.Dir.ToLower() != "photo") //to do use constant or parameterize
                    {
                        saveAsFiles.Clear();
                        saveAsFiles.Add(tmp2);
                        saveAsFiles.Add(tmp1);
                    }
                }
                else
                {
                    Logger.Write("Input Record", "ReplaceFileSaveJsonSql", 0, "Missing or Extra image files Hard to find Photo /Sign StartRow#" + startRowNo, Logger.ERROR);
                }
                jsonSer = JsonConvert.SerializeObject(this.saveAsFiles);
                jsonSer = jsonSer.Replace("'", "''");
            }
            else
            {
                jsonSer = "{}";
            }
            sbFS.Append(jsonSer).Append('\'');

            return insSql.Replace(FILE_SAVE_PLACEHOLDER, sbFS.ToString());
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

        private void ParseSaveAsFileValues(List<string> allColumns, SaveAsFileDef saveAsFileDefnn, string rowType, string[] cells)
        {
            SaveAsFile asFile = saveAsFileDefnn.GetFileDetailsByRow(rowType);
            if (asFile != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    string aColHdr = allColumns[i];
                    AddDbColValAsFileDet(asFile, cells, i, aColHdr);
                }
            }
        }

        private void AddDbColValAsFileDet(SaveAsFile asFile, string[] cells, int i, string aColHdr)
        {
            foreach (var fileCol in asFile.Columns)
            {
                if (fileCol.ColName.ToLower() == aColHdr.ToLower())
                {
                    SaveAsFileColumn tmpCol = new SaveAsFileColumn()
                    {
                        ColName = fileCol.ColName,
                        Dir = fileCol.Dir,
                        SubDir = fileCol.SubDir,
                        FileName = fileCol.FileName,
                        FileContent = cells[i],
                        PhysicalPath = ""
                    };
                    saveAsFiles.Add(tmpCol);// new KeyValuePair<string, string>(dbCol.Value, cells[i]));
                    break;
                }
            }
        }

        private void GetMappedColValues(string pgConnection, string pgSchema, string logProgName, string moduleName, JsonInputFileDef jDef
            , int jobId, int startRowNo)
        {
            string srcVal = "";
            for (int i = 0; i < jDef.mappedColDefnn.MappedColList.Count; i++)
            {
                var col = jDef.mappedColDefnn.MappedColList[i];
                bool hasVal = GetInputVal(col.RowType, col.Index0, col.SourceCol, out srcVal);
                if (hasVal == false)
                {
                    Logger.WriteInfo("Lite Input", "GetMappedColValues", 0,"col map not found " + col.RowType + "-" + col.Index0 + "-" + col.SourceCol);
                    continue;
                }

                string sqlRead = col.GetSqlStr(pgSchema);
                string val = DbUtil.GetMappedVal(pgConnection, logProgName, moduleName, jobId, startRowNo, sqlRead, srcVal);

                mappedCols.Add(new MappedColWithVal()
                {
                    theDef = col,
                    mappedResult = val
                });
                if (allDerivedColVal.ContainsKey(col.DestCol) == false)
                    allDerivedColVal.Add(col.DestCol, val);
            }
        }

        private void GetSequenceValues(bool withLock, string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId
            , JsonInputFileDef jDef, Dictionary<string, string> evalCols, string runFor, string cardType)
        {
            string srcVal;
            string runForParam, freqType, cardTypeParam;
            foreach (SequenceCol col in jDef.sequenceColDefnn.SequenceColList)
            {
                bool hasVal = GetInputVal(col.RowType, col.Index0, col.SourceCol, out srcVal);
                if (hasVal == false)
                {
                    if (evalCols.ContainsKey(col.SourceCol))
                    {
                        hasVal = true;
                        srcVal = evalCols[col.SourceCol];
                    }
                }
                if (hasVal == false)
                    continue;

                runForParam = "";
                freqType = "";
                cardTypeParam = ConstantBag.CARD_NA;

                if (col.Frequency == SequenceCol.DAILY)
                {
                    runForParam = runFor;
                    freqType = SequenceCol.DAILY;
                }
                if (col.ByCardType == 1)
                    cardTypeParam = cardType;

                string pattern = "";
                string newSeq = SequenceGen.GetNextSequence(withLock, pgConnection, pgSchema, col.SequenceMasterType, srcVal, cardTypeParam
                    , ref pattern, col.SeqLength, freqType: freqType, freqValue: runForParam);

                if (pattern != "")
                {
                    string tmp1 = newSeq;
                    string tmp2 = "";
                    if (pattern.IndexOf("{chk_val}") > 0)
                    {
                        tmp2 = GetCheckDigit(pgConnection, pgSchema, logProgName, moduleName, jobId, courierId: srcVal, seqNumAsStr: tmp1);
                    }

                    newSeq = pattern.Replace("{sequence}", tmp1)
                        .Replace("{chk_val}", tmp2);
                }

                this.sequenceCols.Add(new SequenceColWithVal()
                {
                    DestCol = col.DestCol,
                    SequenceSrc = srcVal,
                    SequenceStr = newSeq
                });
                if (allDerivedColVal.ContainsKey(col.DestCol) == false)
                    allDerivedColVal.Add(col.DestCol, newSeq);
            }
        }

        private string GetCheckDigit(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, string courierId, string seqNumAsStr)
        {
            string awbParams;
            if (tmpCourrAWB.ContainsKey(courierId))
                awbParams = tmpCourrAWB[courierId];
            else
            {
                awbParams = DbUtil.GetAWBParams(pgConnection, pgSchema, logProgName, moduleName, jobId, courierId);
                tmpCourrAWB[courierId] = awbParams;
            }

            if (string.IsNullOrEmpty(awbParams))
                return "";

            string[] theParams = awbParams.Split(':');
            int magicDiv = 11;
            if (theParams.Length > 1)
                int.TryParse(theParams[1], out magicDiv);

            string workSeq = seqNumAsStr;
            string workKey = theParams[0];

            if (workKey.Length < workSeq.Length)
            {
                workSeq = seqNumAsStr.Substring(0, workKey.Length);
            }

            int sumOfMult = 0;
            int theZeroCh = (int)('0');
            char[] seqChArr = workSeq.ToCharArray();
            char[] keyChArr = workKey.ToCharArray();
            for (int i = 0; i < workSeq.Length; i++)
            {
                int s1 = (int)seqChArr[i] - theZeroCh;
                int s2 = (int)keyChArr[i] - theZeroCh;
                sumOfMult += s1 * s2;
            }
            int rMod = sumOfMult % magicDiv;
            rMod = magicDiv - rMod;
            if (rMod > 9)
                rMod = rMod % 9;

            return rMod.ToString();
        }
    }

}
