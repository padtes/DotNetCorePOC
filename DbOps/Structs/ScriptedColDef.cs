using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class ScriptCol
    {
        [JsonProperty("dest_col")]
        public string DestCol { get; set; }

        [JsonProperty("script_file")]
        public string ScriptFile { get; set; }

        [JsonProperty("script")]
        public string Script { get; set; }
    }
    public class ScriptedColDef
    {
        public List<ScriptCol> ScriptColList { get; set; }
    }
}
