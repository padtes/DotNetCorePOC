using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SystemWord : SystemParamReport
    {
        //[JsonProperty("file_type")]
        //public string FileType { get; set; }

        //[JsonProperty("write_type")]
        //public string WriteType { get; set; }

        //[JsonProperty("data_table_name")]
        //public string DataTableName { get; set; }

        //[JsonProperty("data_table_json_col")]
        //public string DataTableJsonCol { get; set; }

        //[JsonProperty("data_where")]
        //public string DataWhere { get; set; }

        //[JsonProperty("data_order")]
        //public string DataOrder { get; set; }

        //[JsonProperty("where_cols")]
        //public List<object> WhereCols { get; set; }

        [JsonProperty("word_all_files_dir")]
        public string WordAllFilesDir { get; set; }

        [JsonProperty("word_header_file")]
        public string WordHeaderFile { get; set; }

        [JsonProperty("word_footer_file")]
        public string WordFooterFile { get; set; }

        [JsonProperty("word_middle_page")]
        public string WordMiddlePage { get; set; }

        [JsonProperty("word_work_dir")]
        public string WordWorkDir { get; set; }

        [JsonProperty("max_pages_per_file")]
        public int MaxPagesPerFile { get; set; }
    }
    public class Placeholder : ColumnDetail
    {
    //    [JsonProperty("col")]
    //    public string ColNumber { get; set; }

    //    [JsonProperty("tag")]
    //    public string Tag { get; set; }

    //    [JsonProperty("src_type")]
    //    public string SrcType { get; set; }

    //    [JsonProperty("db_value")]
    //    public string DbValue { get; set; }

    //    [JsonProperty("alias")]
    //    public string Alias { get; set; }

    //    [JsonProperty("print_yn")]
    //    public string PrintYN { get; set; }
    }

    public class CommentsOnUsageWord
    {
        [JsonProperty("0")]
        public string _0 { get; set; }

        [JsonProperty("1")]
        public string _1 { get; set; }

        [JsonProperty("2")]
        public string _2 { get; set; }
    }
    public class RootJsonParamWord
    {
        [JsonProperty("comments_on_usage")]
        public CommentsOnUsageWord CommentsOnUsage { get; set; }

        [JsonProperty("system_word")]
        public SystemWord SystemWord { get; set; }

        [JsonProperty("placeholders")]
        public List<Placeholder> Placeholders { get; set; }
    }

}
