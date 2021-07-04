using DbOps;
using DbOps.Structs;
using Newtonsoft.Json;
using NpsScriban;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessor
{
    public abstract class InputRecordAbs
    {
        public List<KeyValuePair<string, string>> DbColsWithVals = new List<KeyValuePair<string, string>>();

        public abstract bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
               , Dictionary<string, List<string>> jsonSkip, SaveAsFileDef saveAsFileDefnn
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
        public string GetColumnValue(string theColName)
        {
            string valToCheck = "";
            foreach (var dbColVal in DbColsWithVals)
            {
                if (dbColVal.Key.Equals(theColName, StringComparison.InvariantCultureIgnoreCase))
                {
                    valToCheck = dbColVal.Value;
                    break;
                }
            }

            return valToCheck;
        }

        private void AddDbColVal(List<KeyValuePair<string, string>> dbMap, string[] cells, int i, string aColHdr)
        {
            foreach (var dbCol in dbMap)  //Note - no LINQ for KeyValuePair
            {
                if (dbCol.Key.ToLower() == aColHdr.ToLower())
                {
                    DbColsWithVals.Add(new KeyValuePair<string, string>(dbCol.Value, cells[i]));
                    break;
                }
            }
        }
    }

    public class InputHeader : InputRecordAbs
    {
        public List<KeyValuePair<string, string>> hdrFields = new List<KeyValuePair<string, string>>();

        public override bool HandleRow(List<string> allColumns, Dictionary<string, List<KeyValuePair<string, string>>> dbMapDict
                , Dictionary<string, List<string>> jsonSkip, SaveAsFileDef saveAsFileDefnn
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
            JsonColValPairs = new List<KeyValuePair<string, string>>();
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

            for (int i = 0; i < JsonColValPairs.Count; i++)
            {
                var jPair = JsonColValPairs[i];
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
            JsonColsWithVals = new List<JsonColsWithVals>();
        }

        internal void HandleRow(List<string> allColumns, List<string> skipJsonColumns, string[] cells)
        {
            JsonColsWithVals jColVals = new JsonColsWithVals();

            jColVals.HandleRow(allColumns, skipJsonColumns, cells);

            JsonColsWithVals.Add(jColVals);
        }

        internal void RenderJson(StringBuilder sb2)
        {
            sb2.Append('[');
            bool first = true;

            for (int i = 0; i <= JsonColsWithVals.Count; i++)
            {
                var jColVal = JsonColsWithVals[i];
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
        public Dictionary<string, JsonArrOfColsWithVals> JsonByRowType = new Dictionary<string, JsonArrOfColsWithVals>();
        public List<SaveAsFileColumn> saveAsFiles = new List<SaveAsFileColumn>();

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
            , JsonInputFileDef jDef, int jobId, int startRowNo, int fileinfoId, InputHeader inputHdr)
        {
            string dataTableName = jDef.inpSysParam.DataTableName;
            string jsonColName = jDef.inpSysParam.DataTableJsonCol;

            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb1.Append("insert into ").Append(pgSchema).Append('.').Append(dataTableName);
            sb1.Append("(fileinfo_id, row_number, files_saved");
            sb2.Append(fileinfoId).Append(',').Append(startRowNo).Append(',').Append(GetFileSaveJsonSql());

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
            GenerateMappedColumnPart(pgConnection, pgSchema, logProgName, moduleName, jDef, jobId, startRowNo, first, jStr);
            //done mapped columns
            jStr.Append('}');

            sb2.Append(jStr.Replace("'", "''"));
            sb2.Append('\'');
            sb1.Append(") values (").Append(sb2).Append(')');

            return sb1.ToString();
        }

        private void GenerateMappedColumnPart(string pgConnection, string pgSchema, string logProgName, string moduleName
            , JsonInputFileDef jDef, int jobId, int startRowNo, bool hasNoOtherRows, StringBuilder jStr)
        {
            if (hasNoOtherRows == false)
            {
                jStr.Append(',');
            }
            jStr.Append('"').Append("xx").Append("\":"); //extended mapped and scripted values
            jStr.Append('[');
            bool first = true;
            string srcVal;

            for (int i = 0; i < jDef.mappedColDefnn.MappedColList.Count; i++)
            {
                if (first == false)
                {
                    jStr.Append(',');
                }
                var col = jDef.mappedColDefnn.MappedColList[i];
                bool hasVal = GetInputVal(col, out srcVal);
                if (hasVal == false)
                    continue;

                AddToJsonFromMapped(pgConnection, pgSchema, logProgName, moduleName, jobId, startRowNo, jStr, srcVal, col);
                first = false;
            }

            string almostWholeJson = jStr.ToString() + "]}";
            AddToJsonFromScriban(jStr, first, jDef, almostWholeJson);

            jStr.Append(']');
        }

        private void AddToJsonFromScriban(StringBuilder jStr, bool hasNoOtherRows, JsonInputFileDef jDef, string almostWholeJson)
        {
            bool first = hasNoOtherRows;
            foreach (ScriptCol scrptCol in jDef.scrpitedColDefnn.ScriptColList)
            {
                string scriptToRun = scrptCol.Script;
                if (string.IsNullOrEmpty(scriptToRun))
                {
                    scriptToRun = scrptCol.ScriptFile; //to do load the file
                }
                string val = ScribanHandler.Generate(scrptCol.DestCol, almostWholeJson, scriptToRun, false, false);
                if (first == false)
                {
                    jStr.Append(',');
                }
                jStr.Append('"').Append(scrptCol.DestCol).Append("\":");
                jStr.Append('"').Append(val).Append("\"");
                first = false;
            }
        }

        private static void AddToJsonFromMapped(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, int startRowNo
            , StringBuilder jStr, string srcVal, MappedCol col)
        {
            string sqlRead = col.GetSqlStr(pgSchema);
            string val = DbUtil.GetMappedVal(pgConnection, logProgName, moduleName, jobId, startRowNo, sqlRead, srcVal);

            jStr.Append('"').Append(col.DestCol).Append("\":");
            jStr.Append('"').Append(val).Append("\"");
        }

        private bool GetInputVal(MappedCol col, out string srcVal)
        {
            srcVal = "";
            if (JsonByRowType.ContainsKey(col.RowType) == false)
            {
                return false;
            }
            JsonArrOfColsWithVals row = JsonByRowType[col.RowType];
            if (row.JsonColsWithVals == null || row.JsonColsWithVals.Count < col.Index0 + 1)
            {
                return false;
            }
            JsonColsWithVals jColVal = row.JsonColsWithVals[col.Index0];

            foreach (KeyValuePair<string, string> kv in jColVal.JsonColValPairs)
            {
                if (kv.Key == col.SourceCol)
                {
                    srcVal = kv.Value;
                    return true;
                }
            }
            return false;
        }

        private string GetFileSaveJsonSql()
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

            return sbFS.ToString();
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
    }

}
