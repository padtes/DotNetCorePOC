using Newtonsoft.Json;

namespace WriteDocXML
{
    public class ColumnDetail
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("src_type")]
        public string SrcType { get; set; }

        [JsonProperty("db_value")]
        public string DbValue { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }
    }


}
