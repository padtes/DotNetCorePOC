using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class SystemParam
    {
        public static string FileType { get; set; }
        public static int RowTypeIndex { get; set; }
        public static string FileHeaderRowType { get; set; }
        public static string DataRowType { get; set; }
        public static string DataTableName { get; set; }
        public static string DataTableJsonCol { get; set; }
    }

}
