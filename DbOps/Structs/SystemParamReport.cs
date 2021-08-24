using Newtonsoft.Json;
using System.Collections.Generic;

namespace DbOps.Structs
{
    [JsonObject("System")]
    public class SystemParamReport
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
    public class ColumnDetail
    {
        [JsonProperty("col")]
        public string ColNumber { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("src_type")]
        public string SrcType { get; set; }

        [JsonProperty("db_value")]
        public string DbValue { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }
 
        [JsonProperty("print_yn")]
        public string PrintYN { get; set; }
    }

    public class CommentsOnUsageCSV
    {
        [JsonProperty("0")]
        public string _0 { get; set; }

        [JsonProperty("1")]
        public string _1 { get; set; }

        [JsonProperty("2")]
        public string _2 { get; set; }

        [JsonProperty("3")]
        public string _3 { get; set; }

        [JsonProperty("3.1")]
        public string _31 { get; set; }

        [JsonProperty("3.2a")]
        public string _32a { get; set; }

        [JsonProperty("3.2b")]
        public string _32b { get; set; }

        [JsonProperty("3.2c")]
        public string _32c { get; set; }

        [JsonProperty("3.3")]
        public string _33 { get; set; }
    }

    public class SystemParamCSV : SystemParamReport
    {
        [JsonProperty("delimt")]
        public string Delimt { get; set; }

        [JsonProperty("number_of_blanks")]
        public string NumberOfBlankStr { get; set; }
        public int GetNumberOfBlankLines()
        {
            if (string.IsNullOrEmpty(NumberOfBlankStr))
                return 0;

            int cnt;
            NumberOfBlankStr =NumberOfBlankStr.Trim();
            if (int.TryParse(NumberOfBlankStr, out cnt))
                return cnt;

            return 0;
        }

        [JsonProperty("text_qualifier")]
        public string TextQualifier { get; set; }

        [JsonProperty("escape_qualifier")]
        public string EscQualifier { get; set; }
    }

    public class RootJsonParamCSV
    {
        [JsonProperty("comments_on_usage")]
        public CommentsOnUsageCSV CommentsOnUsage { get; set; }

        [JsonProperty("system")]
        public SystemParamCSV System { get; set; }

        [JsonProperty("header")]
        public List<ColumnDetail> Header { get; set; }

        [JsonProperty("detail")]
        public List<ColumnDetail> Detail { get; set; }
    }

}
