using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SaveAsFileDef
    {
        public List<SaveAsFile> SaveAsFile { get; set; }
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

    public class SaveAsFileColumnDict
    {
        Dictionary<string, SaveAsFileColumn> keyValueColDets { get; set; } //string is colName
    }

    public class SaveAsFileDefDict
    {
        Dictionary<string, SaveAsFileColumnDict> keyValueCols { get; set; } //string is rowType
    }

}
