using System.Data;

namespace DbOps
{
    public class LiteApyDbOps
    {
        public static DataSet GetDataset(string pgConnection, string pgSchema,  string moduleName, string bizType, int jobId, string myStatus)
        {
            string sql = $"select * from {pgSchema}.fileinfo where isdeleted='0' and biztype='{bizType}' and module_name='{moduleName}' and inp_rec_status= '{myStatus}' order by id";

            return DbUtil.GetDataSet(pgConnection, bizType + "_GetDataset", moduleName, jobId, sql); 
        }

    }
}


