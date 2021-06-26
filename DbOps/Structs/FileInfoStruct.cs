using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps.Structs
{
    public class FileInfoStruct
    {
        public int id { get; set; }
        public string fname { get; set; }
        public string fpath { get; set; }
        public int fsize { get; set; }

        public string bizType { get; set; }
        public string moduleName { get; set; }
        public string direction { get; set; }
        public string importedFrom { get; set; }
        public string courierSname { get; set; }
        public string courierMode { get; set; }
        public int nprodRecords { get; set; }
        //nrecords
        public string archivePath { get; set; }
        public int archiveAfter { get; set; }
        public int purgeAfter { get; set; }

        public DateTime addedDate { get; set; }
        public string addedBy { get; set; }
        public string addedfromIP { get; set; }
        public DateTime updateDate { get; set; }
        public string updatedBy { get; set; }
        public string updatedFromIP { get; set; }
        public bool isDeleted { get; set; }
        public string inpRecStatus { get; set; }
        public DateTime inpRecStatusDtUTC { get; set; }
        //meta
    }
}
