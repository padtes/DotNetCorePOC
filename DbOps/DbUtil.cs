using Logging;
using Npgsql;
using System;
using System.Data;

namespace DbOps
{
    public class DbUtil
    {
        public static DataSet GetDataSet(string pgConnection, string bizType, string moduleName, int jobId, string sql)
        {
            try
            {
                DataSet ds = new DataSet();
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
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
                Logger.Write(bizType + "_" + moduleName, "GetDataSet", jobId, "Error Sql:" + sql, Logger.WARNING);
                Logger.WriteEx(bizType + "_" + moduleName, "GetDataSet", jobId, ex);
                throw;
            }
        }
        public static bool ExecuteNonSql(string pgConnection, string bizType, string moduleName, int jobId, int rowNum, string sql)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
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
                Logger.Write(bizType + "_" + moduleName, "ExecuteInsert", jobId, $"Error inserting before row {rowNum}, see sql below", Logger.WARNING);
                Logger.Write(bizType + "_" + moduleName, "ExecuteInsert", jobId, "Error Sql:" + sql, Logger.WARNING);
                Logger.WriteEx(bizType + "_" + moduleName, "ExecuteInsert", jobId, ex);
                throw;
            }
            return true;
        }

        public static bool CanConnectToDB(string pgConnection)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
                {
                    conn.Open();
                    conn.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write("DbUtil", "CanConnectToDB", 0, "for connStr:" + pgConnection, Logger.ERROR);
                Logger.WriteEx("DbUtil", "CanConnectToDB", 0, ex);
            }
            return false;

        }

        public static string GetParamsJsonStr(string pgConnection, string pgSchema, string bizType, string moduleName)
        {
            try
            {
                string utc = DateTime.UtcNow.ToString("yyyy/M/d HH:mm:ss");
                string sql = $"select params_json from {pgSchema}.system_param where biztype = '{bizType}' and" +
                    $" module_name='{moduleName}' and '{utc}' >= start_ts_utc and (end_ts_utc is null or {utc} <= end_ts_utc);";
                DataSet ds = GetDataSet(pgConnection, bizType, "GetParamsJsonStr", 0, sql);

                if (ds.Tables.Count > 0)
                {
                    string sysParam = Convert.ToString(ds.Tables[0].Rows[0][0]);
                    return sysParam;
                }
            }
            catch
            {
                // do nothing  - invalid index / null / dbNull
            }
            return "";
        }
    }
}
