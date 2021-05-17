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
        public int FileDefId { get; set; }
    }
}
