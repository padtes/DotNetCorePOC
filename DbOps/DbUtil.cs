using Logging;
using Npgsql;
using System;
using System.Data;

namespace DbOps
{
    public class DbUtil
    {
        public static DataSet GetDataSet(string pgConStr, string moduleName, int jobId, string sql)
        {
            try
            {
                DataSet ds = new DataSet();
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConStr))
                {
                    conn.Open();

                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);

                        da.Fill(ds);
                    }
                }
                return ds;
            }
            catch (Exception ex)
            {
                Logger.Write(moduleName, "GetDataSet", jobId, "Error Sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "GetDataSet", jobId, ex);
                throw;
            }
        }
        public static bool ExecuteNonSql(string pgConStr, string moduleName, int jobId, int rowNum, string sql)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(moduleName, "ExecuteInsert", jobId, $"Error inserting before row {rowNum}, see sql below", Logger.WARNING);
                Logger.Write(moduleName, "ExecuteInsert", jobId, "Error Sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "ExecuteInsert", jobId, ex);
                throw;
            }
            return true;
        }

    }
}
