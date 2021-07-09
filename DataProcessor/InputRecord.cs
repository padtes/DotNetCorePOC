using DbOps;
using DbOps.Structs;
using Newtonsoft.Json;
using NpsScriban;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessor
{
    public class InputRecord : InputRecordAbs
    {
        private const string FILE_SAVE_PLACEHOLDER = "###FILESAVE###";

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
            , string sysPath, JsonInputFileDef jDef, int jobId, int startRowNo, int fileinfoId, InputHeader inputHdr)
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
            GenerateMappedColumnPart(pgConnection, pgSchema, logProgName, moduleName, sysPath, jDef, jobId, startRowNo, first, jStr);
            //done mapped columns
            jStr.Append('}');

            sb2.Append(jStr.Replace("'", "''"));
            sb2.Append('\'');
            sb1.Append(") values (").Append(sb2).Append(')');

            return sb1.ToString();
        }

        private void GenerateMappedColumnPart(string pgConnection, string pgSchema, string logProgName, string moduleName
            , string sysPath, JsonInputFileDef jDef, int jobId, int startRowNo, bool hasNoOtherRows, StringBuilder jStr)
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

            AddToJsonFromSequences(jStr, ref first);

            string almostWholeJson = jStr.ToString() + "}}";
            AddToJsonFromScriban(jStr, first, sysPath, jDef, almostWholeJson);

            jStr.Append('}'); //end "xx"
        }

        private void AddToJsonFromSequences(StringBuilder jStr, ref bool hasNoOtherRows)
        {
            foreach (SequenceColWithVal seqCol in this.sequenceCols)
            {
                if (hasNoOtherRows == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(seqCol.DestCol).Append("\":");
                jStr.Append('"').Append(seqCol.SequenceStr).Append("\"");
                hasNoOtherRows = false;
            }
        }

        private void AddToJsonFromScriban(StringBuilder jStr, bool hasNoOtherRows, string sysPath, JsonInputFileDef jDef, string almostWholeJson)
        {
            bool first = hasNoOtherRows;
            foreach (ScriptCol scrptCol in jDef.scrpitedColDefnn.ScriptColList)
            {
                string val = ScribanHandler.Generate(sysPath, scrptCol, almostWholeJson, false, false);
                if (first == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(scrptCol.DestCol).Append("\":");
                jStr.Append('"').Append(val).Append("\"");
                first = false;

                if (allDerivedColVal.ContainsKey(scrptCol.DestCol) == false)
                    allDerivedColVal.Add(scrptCol.DestCol, val);
            }
        }

        internal void PrepareColumns(string pgConnection, string pgSchema, string logProgName, string moduleName, JsonInputFileDef jDef, int jobId, int startRowNo)
        {
            GetSequenceValues(pgConnection, pgSchema, jDef);
            GetMappedColValues(pgConnection, pgSchema, logProgName, moduleName, jDef, jobId, startRowNo);
        }

        private static void AddToJsonFromMapped(StringBuilder jStr, MappedColWithVal col)
        {
            jStr.Append('"').Append(col.theDef.DestCol).Append("\":");
            jStr.Append('"').Append(col.mappedResult).Append("\"");
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

        public string ReplaceFileSaveJsonSql(string insSql)
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
                    continue;

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

        private void GetSequenceValues(string pgConnection, string pgSchema, JsonInputFileDef jDef)
        {
            string srcVal;
            foreach (SequenceCol col in jDef.sequenceColDefnn.SequenceColList)
            {
                bool hasVal = GetInputVal(col.RowType, col.Index0, col.SourceCol, out srcVal);
                if (hasVal == false)
                    continue;

                string newSeq = SequenceGen.GetNextSequence(pgConnection, pgSchema, col.SequenceMasterType, srcVal, col.SeqLength);

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


    }

}
