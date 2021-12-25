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

        public string moduleName { get; set; }
        public string bizType { get; set; }
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

    public class FileInfoStructPAN : FileInfoStruct
    {
        public int ParentId { get; set; }
        public int LocalIndex { get; set; }
    }
    public class FileTypeMaster
    {
        public int id { get; set; }
        public bool isActive { get; set; }
        public string moduleName { get; set; }
        public string bizType { get; set; }
        public int archiveAfter { get; set; }
        public int purgeAfter { get; set; }
        public string fnamePattern { get; set; }
        public string fnamePatternAttr { get; set; }
        public string fnamePatternName { get; set; }
        public string ext { get; set; }
        public string fType { get; set; }
        public string fileDefJson { get; set; }
        public string fileDefJsonFName { get; set; }

    }

    public class FileDetailStruct
    {
        public int Id { get; set; }

        public int FileinfoId { get; set; }
        public string ProdId { get; set; }
        public string CourierId { get; set; }
        public string Jstr { get; set; }
        public int StartRowNumber { get; set; }
        public string AckNumber { get; set; }
        public string ApyFlag { get; set; }
        public string FilesSavedJstr { get; set; }

        // public List<FiledetailActionStruct> actions { get; set; }
    }

    //public class FiledetailActionStruct
    //{
    //    public int Id { get; set; }
    //    public int FiledetId { get; set; }
    //    public string ActionDone { get; set; }
    //    public bool IsVoid { get; set; }
    //}
}
