using DbOps.Structs;
using Logging;
using Npgsql;
using System;
using System.Data;

namespace DbOps
{
    public class DbUtil
    {
        public static DataSet GetDataSet(string pgConnection, string logProgramName, string moduleName, int jobId, string sql)
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
                LogSqlError(moduleName, logProgramName, "GetDataSet", jobId, 0, sql, ex);
                throw;
            }
        }

        public static bool ExecuteNonSql(string pgConnection, string logProgramName, string moduleName, int jobId, int rowNum, string sql)
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
                LogSqlError(moduleName, logProgramName, "ExecuteNonSql", jobId, rowNum, sql, ex);
                throw;
            }
            return true;
        }

        public static bool Unlock(string pgSchema, string pgConnection)
        {
            string sql = $"update {pgSchema}.counters set lock_key='0' where  lock_key > '0' and parent_id > 0";
            return ExecuteNonSql(pgConnection, "UNLOCK", "all", 0, 0, sql);
        }

        public static bool ExecuteScalar(string pgConnection, string logProgramName, string moduleName, int jobId, int rowNum, string sql, out int pkId)
        {
            pkId = -1;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        var res = cmd.ExecuteScalar();
                        pkId = Convert.ToInt32(res);
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgramName, "ExecuteScalar", jobId, rowNum, sql, ex);
                throw;
            }
            return true;
        }

        private static void LogSqlError(string moduleName, string logProgName, string methodNm, int jobId, int rowNum, string sql, Exception ex)
        {
            Logger.Write(moduleName + "_" + logProgName, methodNm, jobId, $"Error in row {rowNum}, see sql below", Logger.WARNING);
            Logger.Write(moduleName + "_" + logProgName, methodNm, jobId, "Error Sql:" + sql, Logger.WARNING);
            Logger.WriteEx(moduleName + "_" + logProgName, methodNm, jobId, ex);
        }

        public static bool IsRecFound(string pgConnection, string moduleName, string logProgName, int jobId, int rowNum, string sql, bool getId, out int id)
        {
            bool recFound = false;
            id = -1;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            recFound = true;
                            if (getId)
                            {
                                id = rdr.GetInt32(0);
                            }
                            break;
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgName, "IsRecFound", jobId, rowNum, sql, ex);
                throw;
            }
            return recFound;
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

        public static string GetParamsJsonStr(string pgConnection, string pgSchema, string moduleName, string bizType, int jobId, string logProgramName)
        {
            try
            {
                string utc = DateTime.UtcNow.ToString("yyyy/M/d HH:mm:ss");
                string sql = $"select params_json from {pgSchema}.system_param where biztype = '{bizType}' and" +
                    $" module_name='{moduleName}' and '{utc}' >= start_ts_utc and (end_ts_utc is null or '{utc}' <= end_ts_utc);";

                DataSet ds = GetDataSet(pgConnection, logProgramName+ "_GetParamsJsonStr", moduleName, jobId, sql);

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

        public static string MyEscape(string inVal)
        {
            return inVal.Replace("'", "''");
        }

        public static void UpsertFileInfo(string pgConnection, string moduleName, string logProgName, int jobId, int rowNum, string pgSchema, bool reprocess, FileInfoStruct theFile, out string actionTaken)
        {
            //if reprocess == true and record found - update status = TO DO and dateTime of status update, overwrite file from input to work
            //if reprocess == false and record found - ignore

            actionTaken = "";

            string sql = $"select id from {pgSchema}.fileinfo where fname='{MyEscape(theFile.fname)}' and fpath='{MyEscape(theFile.fpath)}'";
            bool isUpdate = IsRecFound(pgConnection, moduleName, logProgName, jobId, rowNum, sql, true, out int id);

            if (isUpdate && reprocess == false)
            {
                actionTaken = CommonUtil.ConstantBag.IGNORED;
                return;  //----------------------------------
            }

            try
            {
                if (isUpdate)
                {
                    theFile.id = id;
                    UpdateFileInfoStatus(pgConnection, pgSchema, theFile, ref sql);
                    actionTaken = CommonUtil.ConstantBag.UPDATED;
                }
                else
                {
                    InsertFileInfo(pgConnection, pgSchema, theFile, ref sql);
                    actionTaken = CommonUtil.ConstantBag.INSERTED;
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgName, "InsertFileInfo", jobId, rowNum, sql, ex);
                throw;
            }
        }

        private static string UpdateFileInfoStatus(string pgConnection, string pgSchema, FileInfoStruct theFile, ref string sql)
        {
            sql = $"update {pgSchema}.fileinfo set inp_rec_status = '{theFile.inpRecStatus}', inp_rec_status_ts_utc='{theFile.inpRecStatusDtUTC}', isdeleted='{theFile.isDeleted}'" +
                $" where id='{theFile.id}'";
            using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            return sql;
        }

        private static string InsertFileInfo(string pgConnection, string pgSchema, FileInfoStruct theFile, ref string sql)
        {
            sql = $"insert into {pgSchema}.fileinfo (fname, fpath, fsize, biztype, module_name, direction, importedfrom, courier_sname, courier_mode, " +
                "nprodrecords, archivepath, archiveafter, purgeafter, addeddate, addedby, addedfromip, updatedate, " +
                "updatedby, updatedfromip, isdeleted, inp_rec_status, inp_rec_status_ts_utc) " +
                $"values (@fname, @fpath, @fsize, @biztype, @module_name, @direction, @importedfrom, @courier_sname, @courier_mode, " +
                "@nprodrecords, @archivepath, @archiveafter, @purgeafter, @addeddate, @addedby, @addedfromip, @updatedate, " +
                "@updatedby, @updatedfromip, @isdeleted, @inp_rec_status, @inp_rec_status_ts_utc" +
                $") RETURNING id";

            using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@fname", theFile.fname);
                    cmd.Parameters.AddWithValue("@fpath", theFile.fpath);
                    cmd.Parameters.AddWithValue("@fsize", theFile.fsize);
                    cmd.Parameters.AddWithValue("@biztype", theFile.bizType);
                    cmd.Parameters.AddWithValue("@module_name", theFile.moduleName);
                    cmd.Parameters.AddWithValue("@direction", theFile.direction);
                    cmd.Parameters.AddWithValue("@importedfrom", theFile.importedFrom);
                    cmd.Parameters.AddWithValue("@courier_sname", theFile.courierSname);
                    cmd.Parameters.AddWithValue("@courier_mode", theFile.courierMode);
                    cmd.Parameters.AddWithValue("@nprodrecords", theFile.nprodRecords);
                    cmd.Parameters.AddWithValue("@archivepath", theFile.archivePath);
                    cmd.Parameters.AddWithValue("@archiveafter", theFile.archiveAfter);
                    cmd.Parameters.AddWithValue("@purgeafter", theFile.purgeAfter);
                    cmd.Parameters.AddWithValue("@addeddate", theFile.addedDate);
                    cmd.Parameters.AddWithValue("@addedby", theFile.addedBy);
                    cmd.Parameters.AddWithValue("@addedfromip", theFile.addedfromIP);
                    cmd.Parameters.AddWithValue("@updatedate", theFile.updateDate);
                    cmd.Parameters.AddWithValue("@updatedby", theFile.updatedBy);
                    cmd.Parameters.AddWithValue("@updatedfromip", theFile.updatedFromIP);
                    cmd.Parameters.AddWithValue("@isdeleted", theFile.isDeleted);
                    cmd.Parameters.AddWithValue("@inp_rec_status", theFile.inpRecStatus);
                    cmd.Parameters.AddWithValue("@inp_rec_status_ts_utc", theFile.inpRecStatusDtUTC);

                    conn.Open();
                    var res = cmd.ExecuteScalar();
                    int id = Convert.ToInt32(res);
                    theFile.id = id;
                    conn.Close();
                }
            }

            return sql;
        }

        public static FileTypMaster GetFileTypMaster(string pgConnection, string pgSchema, string moduleName, string bizType, int jobId, string myStatus)
        {
            string sql = $"select * from {pgSchema}.fileinfo where isdeleted='0' and biztype='{bizType}' and module_name='{moduleName}' and inp_rec_status= '{myStatus}' order by id";

            DataSet ds = DbUtil.GetDataSet(pgConnection, bizType + "_GetDataset", moduleName, jobId, sql);
            if (ds.Tables.Count < 1 || ds.Tables[0].Rows.Count < 1)
                return null;

            DataRow dr = ds.Tables[0].Rows[0];

            FileTypMaster fm = new FileTypMaster()
            {
                id = Convert.ToInt32(dr["id"]),
                isActive = Convert.ToBoolean(dr["isactive"]),
                bizType = Convert.ToString(dr["biztype"]),
                moduleName = Convert.ToString(dr["module_name"]),
                archiveAfter = Convert.ToInt32(dr["archiveafter"]),
                purgeAfter = Convert.ToInt32(dr["purgeafter"]),
                fnamePattern = Convert.ToString(dr["fname_pattern"]),
                fnamePatternAttr = Convert.ToString(dr["fname_pattern_attr"]),
                fnamePatternName = Convert.ToString(dr["fname_pattern_name"]),
                ext = Convert.ToString(dr["ext"]),
                fType = Convert.ToString(dr["ftype"]),
                fileDefJson = Convert.ToString(dr["file_def_json"]),
                fileDefJsonFName = Convert.ToString(dr["file_def_json_fName"])
            };

            return fm;
        }
    }
}
