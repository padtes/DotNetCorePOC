using Npgsql;
using System;
using Logging;

namespace ReadCSV
{
    internal class DbUtil
    {
        internal static bool ExecuteNonSql(string pgConStr, string moduleName, int jobId, int rowNum, string sql)
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
                return false;
            }
            return true;
        }
    }
}