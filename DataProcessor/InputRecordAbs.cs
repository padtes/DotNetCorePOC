using DbOps.Structs;
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

                sb2.Append('"').Append(jPair.Key).Append("\":\"").Append(EscapeJson(jPair.Value)).Append('"');
                first = false;
            }

            sb2.Append('}');
        }

        public static string EscapeJson(string val)
        {
            return val.Replace("\"", "\\\"");
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

            for (int i = 0; i < JsonColsWithVals.Count; i++)
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

}
