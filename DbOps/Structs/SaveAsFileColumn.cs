using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SaveAsFileDef
    {
        public SaveAsFileDef()
        {
            SaveAsFile = new List<SaveAsFile>();
        }
        public List<SaveAsFile> SaveAsFile { get; set; }

        public SaveAsFileColumn GetFileDetails(string rowType, string colName)
        {
            SaveAsFileColumn retSAC = null;
            foreach (var item in this.SaveAsFile)
            {
                if (item.RowType.Equals(rowType, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var iCol in item.Columns)
                    {
                        if (iCol.ColName.Equals(colName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            retSAC = iCol;
                            break;
                        }
                    }
                }
                if (retSAC != null)
                    break;
            }
            return retSAC;
        }
    }
    public class SaveAsFileColumn
    {
        [JsonProperty("colName")]
        public string ColName { get; set; }

        [JsonProperty("dir")]
        public string Dir { get; set; }

        [JsonProperty("sub_dir")]
        public string SubDir { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }
    }

    public class SaveAsFile
    {
        [JsonProperty("row_type")]
        public string RowType { get; set; }

        [JsonProperty("columns")]
        public List<SaveAsFileColumn> Columns { get; set; }
    }
}
