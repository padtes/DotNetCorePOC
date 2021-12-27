using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SaveAsFileDef
    {
        public List<SaveAsFile> SaveAsFile { get; set; }

        public SaveAsFileDef()
        {
            SaveAsFile = new List<SaveAsFile>();
        }

        public int GetTotFileCount() 
        {
            int cnt = 0;
            foreach (var item in this.SaveAsFile)
            {
                cnt += item.Columns.Count;
            }
            return cnt;
        }

        public SaveAsFile GetFileDetailsByRow(string rowType)
        {
            SaveAsFile retSAF = null;
            foreach (var item in this.SaveAsFile)
            {
                if (item.RowType.Equals(rowType, StringComparison.InvariantCultureIgnoreCase))
                {
                    retSAF = item;
                    break;
                }
            }
            return retSAF;
        }

       
    }
    public class SaveAsFileColumn
    {
        [JsonProperty("col_name")]
        public string ColName { get; set; }

        [JsonProperty("dir")]
        public string Dir { get; set; }

        [JsonProperty("sub_dir")]
        public string SubDir { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonIgnore]
        private string fileContent;
        [JsonIgnore]
        public string FileContent { get => fileContent; set => fileContent = value; }

        [JsonProperty("actual_file_path")]
        public string PhysicalPath { get; set; }
        [JsonProperty("actual_file_name")]

        public string ActualFileName { get; set; }
    }

    public class SaveAsFile
    {
        [JsonProperty("row_type")]
        public string RowType { get; set; }

        [JsonProperty("columns")]
        public List<SaveAsFileColumn> Columns { get; set; }
    }
}
