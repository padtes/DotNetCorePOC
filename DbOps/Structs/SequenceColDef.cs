using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SequenceCol
    {
        [JsonProperty("row_type")]
        public string RowType { get; set; }

        [JsonProperty("index_0")]
        public int Index0 { get; set; }

        [JsonProperty("source_col")]
        public string SourceCol { get; set; }
        [JsonProperty("dest_col")]
        public string DestCol { get; set; }

        [JsonProperty("sequence_master_type")]
        public string SequenceMasterType { get; set; }
        [JsonProperty("seq_length")]
        public int SeqLength { get; set; }
        [JsonProperty("freq")]
        public string Frequency { get; set; }

        [JsonProperty("by_card_type")]
        public Int16 ByCardType { get; set; }
        public const string DAILY = "daily";
        public const string GLOBAL = "global";
    }
    public class SequenceColWithVal : SequenceCol
    {
        public string SequenceSrc { get; set; }
        public string SequenceStr { get; set; }

    }

    public class SequenceColDef
    {
        [JsonProperty("sequence_columns")]
        public List<SequenceCol> SequenceColList { get; set; }

    }
}
