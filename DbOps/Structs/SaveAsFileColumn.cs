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
        [JsonProperty("colName")]
        public string ColName { get; set; }

        [JsonProperty("dir")]
        public string Dir { get; set; }

        [JsonProperty("sub_dir")]
        public string SubDir { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        private string fileContent;
        public string FileContent { get => fileContent; set => fileContent = value; }
        public string PhysicalPath { get; set; }
    }

    public class SaveAsFile
    {
        [JsonProperty("row_type")]
        public string RowType { get; set; }

        [JsonProperty("columns")]
        public List<SaveAsFileColumn> Columns { get; set; }
    }
}
