using Newtonsoft.Json;
using System.Collections.Generic;

namespace WriteDocXML
{
    [JsonObject("System")]
    public class SystemParam
    {
        [JsonProperty("file_type")]
        public string FileType { get; set; }

        [JsonProperty("write_type")]
        public string WriteType { get; set; }

        [JsonProperty("data_table_name")]
        public string DataTableName { get; set; }

        [JsonProperty("data_table_json_col")]
        public string DataTableJsonCol { get; set; }

        [JsonProperty("data_where")]
        public string DataWhere { get; set; }

        [JsonProperty("data_order")]
        public string DataOrderby { get; set; }

        [JsonProperty("where_cols")]
        public List<ColumnDetail> WhereColList { get; set; }
    }



}
