using Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeletePRAN
{
    class DbUtil
    {
        internal static bool CanConnectToDB(string pgConnection)
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

        internal static DataSet GetDsForSql(string pgConnection, string sql)
        {
            DataSet ds = new DataSet();
            using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
            {
                conn.Open();
                using(NpgsqlCommand pgcom = new NpgsqlCommand(sql, conn))
                {
                    pgcom.CommandType = CommandType.Text;
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(pgcom);
                    da.Fill(ds);
                }
                conn.Close();
            }
            return ds;
        }
    }
}
