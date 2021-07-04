using DbOps.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessor
{
    public class JsonInputFileDef
    {
        public Dictionary<string, List<string>> fileDefDict = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> jsonSkip = new Dictionary<string, List<string>>();
        public Dictionary<string, List<KeyValuePair<string, string>>> dbMap = new Dictionary<string, List<KeyValuePair<string, string>>>();
        public SaveAsFileDef saveAsFileDefnn = new SaveAsFileDef();
        public MappedColDef mappedColDefnn = new MappedColDef();
        public ScriptedColDef scrpitedColDefnn = new ScriptedColDef();

        public SystemParamInput inpSysParam = new SystemParamInput();
    }
}
