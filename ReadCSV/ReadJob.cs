using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCSV
{
    /// <summary>
    /// This is structure for database table read_jobs
    /// Read Process starts with an entry in this table
    /// </summary>
    public class ReadJob
    {
        public int Id { get; set; }
        public string Url { get; set; }
        //user id for ftp
        //password for ftp
        public string OutputPath { get; set; }
        public string OutFileName { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ClientId { get; set; }
        public int ReadTemplateId { get; set; }

        public const string PENDING = "PENDING";
        public const string DOWNLOADED = "DOWNLOADED";
        public const string ERR_DOWNLOAD = "ERR_DOWNLOAD";
        public const string ERR_FILE_MISSING = "ERR_FILE_MISSING";
        public const string ERR_TMPL_MISSING = "ERR_TEMPLATE_MISS";

        //public const string IN_ERROR = "IN_ERROR";
        public const string ERR_HDR_MIS = "ERR_HDR_MISMATCH";
        public const string RAW_WRITTEN = "RAW_WRITTEN";

    }
}
