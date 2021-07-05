using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class MappedCol
    {
        private string sql = "";
        [JsonProperty("row_type")]
        public string RowType { get; set; }

        [JsonProperty("index_0")]
        public int Index0 { get; set; }

        [JsonProperty("source_col")]
        public string SourceCol { get; set; }

        [JsonProperty("dest_col")]
        public string DestCol { get; set; }

        [JsonProperty("map_table")]
        public string MapTable { get; set; }

        [JsonProperty("map_key_col")]
        public string MapKeyCol { get; set; }

        [JsonProperty("map_val_col")]
        public string MapValCol { get; set; }

        [JsonProperty("where")]
        public string Where { get; set; }

        public string GetSqlStr(string pgSchema)
        {
            if (sql == "")
            {
                sql = $"select {MapValCol} from {pgSchema}.{MapTable} where {MapKeyCol} = '{{0}}'";
                if (string.IsNullOrEmpty(Where) == false)
                {
                    sql += " and " + Where;
                }
            }
            return sql; 
        }
    }
    public class MappedColDef
    {
        [JsonProperty("mapped_columns")]
        public List<MappedCol> MappedColList { get; set; }
    }
}
