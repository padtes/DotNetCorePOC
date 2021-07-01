using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SystemParamInput
    {
        public string FileType { get; set; }
        public char Delimt { get; set; }
        public int RowTypeIndex { get; set; }
        public string FileHeaderRowType { get; set; }
        public string DataRowType { get; set; }
        public string DataTableName { get; set; }
        public string DataTableJsonCol { get; set; }
        public string UniqueColumn { get; set; }
        public string CourierCol { get; set; }
    }

}
