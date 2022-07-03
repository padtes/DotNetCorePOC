using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SystemParamInput
    {
        public string FileType { get; set; }
        public char Delimt { get; set; }
        public int RowTypeIndex { get; set; }
        public string FileHeaderRowType { get; set; }
        public string DataRowType { get; set; }
        public string DataTableName { get; set; }
        public string DataTableJsonCol { get; set; }
        public string UniqueColumnNm { get; set; }
        public string UniqueValue { get; set; }
        public string CourierCol { get; set; }

        public string FileSaveDirsOrdered { get; set; }
        public bool IsSecondaryFile { get; set; }
        public bool IsSingleFormatFile { get; set; }

        private Dictionary<string, string> MultiFileRecToJsonMap;
        private string _multiFileRecToJsonCsv;
        public string MultiFileRecToJsonPairsCsv {
            get { return _multiFileRecToJsonCsv; }
            set {
                _multiFileRecToJsonCsv = value;
                MultiFileRecToJsonMap = new Dictionary<string, string>();
                var pairs = _multiFileRecToJsonCsv.Split(",");
                foreach (var item in pairs)
                {
                    var kv = item.Split('=');
                    MultiFileRecToJsonMap.Add(kv[0], kv[1]);
                }
            }
        }
        public bool IsValidMultiFileRecToJson(List<string> recs)
        {
            foreach (string recType in recs)
            {
                if (MultiFileRecToJsonMap.ContainsKey(recType)==false)
                    return false;

            }
            return true;
        }
        public string GetJsonTagForRecordType(string recType)
        {
            if (MultiFileRecToJsonMap == null)
                return recType;
            if (MultiFileRecToJsonMap.ContainsKey(recType))
                return MultiFileRecToJsonMap[recType];
            return recType;
        }
    }
}