using CommonUtil;
using DbOps.Structs;
using Logging;
using Npgsql;
using System;
using System.Collections.Generic;
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

        public static DataTable GetDataTab(string pgConnection, string logProgramName, string moduleName, int jobId, string sql)
        {
            try
            {
                DataTable dt = new DataTable();
                using (NpgsqlDataAdapter dataContent = new NpgsqlDataAdapter(sql, pgConnection))
                {
                    dataContent.Fill(dt);
                }
                return dt;
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgramName, "GetDataTable", jobId, 0, sql, ex);
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

        public static bool Unlock(string pgConnection, string pgSchema)
        {
            string sql = $"update {pgSchema}.counters set lock_key='0' where  lock_key > '0' and parent_id > 0";
            return ExecuteNonSql(pgConnection, "UNLOCK", "all", 0, 0, sql);
        }

        public static bool Unlock(string pgConnection, string pgSchema, string masterType, string counterName, int lockedKey)
        {
            string sql = $"update {pgSchema}.counters set lock_key='0' where counter_name = '{counterName}' and lock_key = '{lockedKey}' and parent_id > 0" +
                $" and parent_id in (select id from {pgSchema}.counters where counter_name = '{masterType}' and parent_id = 0)";

            return ExecuteNonSql(pgConnection, "UNLOCK", "child", 0, 0, sql);
        }

        public static bool ExecuteScalar(string pgConnection, string logProgramName, string moduleName, int jobId, int rowNum, string sql, out int pkId, out bool recFound)
        {
            recFound = false;
            pkId = -1;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        object res = cmd.ExecuteScalar();
                        if (res != DBNull.Value)
                        {
                            pkId = Convert.ToInt32(res);
                            recFound = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgramName, "ExecuteScalar", jobId, rowNum, sql, ex);
                return false;
            }
            return true;
        }

        private static void LogSqlError(string moduleName, string logProgName, string methodNm, int jobId, int rowNum, string sql, Exception ex)
        {
            Logger.Write(moduleName + "_" + logProgName, methodNm, jobId, $"Error in row {rowNum}, see sql below", Logger.WARNING);
            Logger.Write(moduleName + "_" + logProgName, methodNm, jobId, "Error Sql:" + sql, Logger.ERROR);
            Logger.WriteEx(moduleName + "_" + logProgName, methodNm, jobId, ex);
        }

        public static string GetMappedVal(string pgConnection, string logProgName, string moduleName, int jobId, int rowNum, string sql, string mapFromVal)
        {
            string sql2 = string.Format(sql, MyEscape(mapFromVal));
            string retVal = mapFromVal;
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnection))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql2, conn))
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            retVal = Convert.ToString(rdr[0]);
                            break;
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgName, "GetMappedVal", jobId, rowNum, sql, ex);
                throw;
            }

            return retVal;
        }

        public static bool IsRecFound(string pgConnection, string logProgName, string moduleName, int jobId, int rowNum, string sql, bool getId, out int id)
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

        public static string GetParamsJsonStr(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizType, int jobId)
        {
            try
            {
                string utc = DateTime.UtcNow.ToString("yyyy/M/d HH:mm:ss");
                string sql = $"select params_json from {pgSchema}.system_param where biztype = '{bizType}' and" +
                    $" module_name='{moduleName}' and '{utc}' >= start_ts_utc and (end_ts_utc is null or '{utc}' <= end_ts_utc);";

                DataSet ds = GetDataSet(pgConnection, logProgName + "_GetParamsJsonStr", moduleName, jobId, sql);

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

        public static void GetFileInfoList(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, List<FileInfoStruct> listFiles, string dateAsDir, string[] inpRecStatus)
        {
            string tmp = String.Join("','", inpRecStatus);
            string sql = $"select id, fname, fpath from {pgSchema}.fileinfo where isdeleted='0' and inp_rec_status in ('{tmp}') and fpath ='{MyEscape(dateAsDir)}'";
            DataSet ds = GetDataSet(pgConnection, logProgName, moduleName, jobId, sql);
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    listFiles.Add(new FileInfoStruct()
                    {
                        id = Convert.ToInt32(dr["id"]),
                        fname =Convert.ToString(dr["fname"]),
                        fpath= Convert.ToString(dr["fpath"])
                    });
                }
            }
        }

        public static DataSet GetFileDetailList(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizTypeToRead, int jobId
            , string colSelection, string waitingAction, string doneAction, string workdirYmd, string wherePart, string orderBy, out string sql)
        {
            sql = $"select {colSelection} from {pgSchema}.filedetails" +
                $" join {pgSchema}.fileinfo on fileinfo.id = filedetails.fileinfo_id" +
                $" where fileinfo.isdeleted='0'" +
                $" and fileinfo.module_name = '{moduleName}'" +
                $" and fileinfo.biztype = '{bizTypeToRead}'" +
                $" and fileinfo.fpath like '%\\\\{workdirYmd}\\\\%'" +
                $" and not exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{waitingAction}')";

            if (string.IsNullOrEmpty(doneAction) == false)
            {
                sql +=$" and exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{doneAction}')";
            }

            if (string.IsNullOrEmpty(wherePart) == false)
            {
                sql += $" and {wherePart}";
            }
            if (string.IsNullOrEmpty(orderBy) == false)
            {
                sql += $" order by {orderBy}";
            }

            Logger.Write(moduleName + "_" + logProgName, "GetFileDetailList", jobId, "Sql:" + sql, Logger.INFO);

            DataSet ds = GetDataSet(pgConnection, logProgName, moduleName, jobId, sql);

            return ds;
        }

        public static DataSet GetCardReport(string pgConnection, string pgSchema, string logProgramName, string moduleName, string bizTypeToRead, int v, string workdirYmd, bool isApy, string courierCd, string waitingAction, string doneAction, out string sql)
        {
            throw new NotImplementedException();
        }

        public static bool AddAction(string pgConnection, string pgSchema, string logProgramName, string moduleName, int jobId, int rowNum, int detailId, string actionDone)
        {
            string addDtUTC = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");

            string sql = $"insert into {pgSchema}.filedetail_actions(action_void, filedet_id, action_done, addeddate)" +
                $" values ('0',{detailId}, '{actionDone}','{addDtUTC}')";
            return ExecuteNonSql(pgConnection, logProgramName, moduleName, jobId, rowNum, sql);
        }

        public static void UpsertFileInfo(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, int rowNum, bool reprocess, FileInfoStruct theFile, out string actionTaken)
        {
            //if reprocess == true and record found - update status = TO DO and dateTime of status update, overwrite file from input to work
            //if reprocess == false and record found - ignore

            actionTaken = "";

            string sql = $"select id from {pgSchema}.fileinfo where fname='{MyEscape(theFile.fname)}' and fpath='{MyEscape(theFile.fpath)}'";
            bool isUpdate = IsRecFound(pgConnection, logProgName, moduleName, jobId, rowNum, sql, true, out int id);

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

        public static string UpdateFileInfoStatus(string pgConnection, string pgSchema, FileInfoStruct theFile, ref string sql)
        {
            theFile.inpRecStatusDtUTC = DateTime.UtcNow;

            sql = $"update {pgSchema}.fileinfo set inp_rec_status = '{theFile.inpRecStatus}', inp_rec_status_ts_utc='{theFile.inpRecStatusDtUTC.ToString("yyyy/MM/dd HH:mm:ss")}', isdeleted='{theFile.isDeleted}'" +
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

        public static FileTypeMaster GetFileTypeMaster(string pgConnection, string pgSchema, string moduleName, string bizType, int jobId)
        {
            string sql = $"select * from {pgSchema}.filetypemaster where isactive='1' and biztype='{bizType}' and module_name='{moduleName}' order by id";

            DataSet ds = DbUtil.GetDataSet(pgConnection, bizType + "_GetDataset", moduleName, jobId, sql);
            if (ds.Tables.Count < 1 || ds.Tables[0].Rows.Count < 1)
                return null;

            DataRow dr = ds.Tables[0].Rows[0];

            FileTypeMaster fm = new FileTypeMaster()
            {
                id = Convert.ToInt32(dr["id"]),
                isActive = Convert.ToBoolean(dr["isactive"]),
                bizType = GetStringDbNullable(dr["biztype"]),
                moduleName = GetStringDbNullable(dr["module_name"]),
                archiveAfter = GetIntDbNullable(dr["archiveafter"]),
                purgeAfter = GetIntDbNullable(dr["purgeafter"]),
                fnamePattern = GetStringDbNullable(dr["fname_pattern"]),
                fnamePatternAttr = GetStringDbNullable(dr["fname_pattern_attr"]),
                fnamePatternName = GetStringDbNullable(dr["fname_pattern_name"]),
                ext = GetStringDbNullable(dr["ext"]),
                fType = GetStringDbNullable(dr["ftype"]),
                fileDefJson = GetStringDbNullable(dr["file_def_json"]),
                fileDefJsonFName = GetStringDbNullable(dr["file_def_json_fName"])
            };

            return fm;
        }
        public static string GetStringDbNullable(object dbVal)
        {
            if (dbVal == DBNull.Value)
                return "";
            return Convert.ToString(dbVal);
        }
        private static int GetIntDbNullable(object dbVal)
        {
            if (dbVal == DBNull.Value)
                return 0;
            return Convert.ToInt32(dbVal);
        }

        public static void GetRejectCodes(string pgConnection, string pgSchema, string logProgramName, string moduleName, int jobId, List<string> rejectCodes)
        {
            string sql = $"select lstid from {pgSchema}.reject_reasons";
            DataSet ds = GetDataSet(pgConnection, logProgramName, moduleName, jobId, sql);
            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    string rejCd = Convert.ToString(dr[0]);
                    rejectCodes.Add(rejCd);
                }
            }
        }

        public static DataSet GetInternalStatusReport(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizTypeToRead, int jobId
            , string workdirYmd, string waitingAction, string doneAction, out string sql)
        {
            sql = $"select row_number() over () rownum, filedetails.id, filedetails.courier_id" + //filedetails.prod_id, 
                ",filedetails.json_data->'pd'->0->>'p010_first_name' fname" +
                ",filedetails.json_data->'pd'->0->>'p011_last_name_surname' lname" +
                ",filedetails.json_data->'xx'->>'x_document_id' docId" +
                ",fileinfo.fpath fpath" +
                ",filedetails.print_dt, filedetails.pickup_dt" +
                ",filedetails.det_err_csv" +
                $",(select 'imm resp sent' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_RESPONSE1}' limit 1) respact"+
                $",(select 'status updated' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_STAT_UPD2}' limit 1) updact"+
                $",(select 'status sent' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_STAT_REP3}' limit 1) statact"+
                $",(select 'letter done' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_WORD_LTR4}' limit 1) ltract"+
                $",(select 'card done' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_CARD_OUT5}' limit 1) cardact"+
                $",(select 'PTC done' from ventura.filedetail_actions a where a.filedet_id = filedetails.id and a.action_done = '{ConstantBag.DET_LC_STEP_PTC_REP6}' limit 1) ptcact"+
                $" from {pgSchema}.filedetails" +
                $" join {pgSchema}.fileinfo on fileinfo.id = filedetails.fileinfo_id" +
                $" where fileinfo.isdeleted='0'" +
                $" and fileinfo.module_name = '{moduleName}'" +
                $" and fileinfo.biztype = '{bizTypeToRead}'" +
                $" and fileinfo.fpath like '%\\\\{workdirYmd}\\\\%'";
            if (string.IsNullOrEmpty(doneAction)==false)
            {
                sql += " and exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{doneAction}')";
            }
            if (string.IsNullOrEmpty(waitingAction)==false)
            {
                sql += " and not exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{waitingAction}')";
            }

            DataSet ds = GetDataSet(pgConnection, logProgName, moduleName, jobId, sql);
            return ds;
        }
        public static bool UpdateDetStatus(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, int rowNum
            , int detId, string prnDtYMD, string pickDtYMD, string errCsv, string actionDone)
        {
            string prnDtStr = (prnDtYMD == "" ? "null" : $"'{prnDtYMD}'");
            string pickDtStr = (pickDtYMD == "" ? "null" : $"'{pickDtYMD}'");

            string sql = $"update {pgSchema}.filedetails set print_dt = {prnDtStr}, pickup_dt={pickDtStr}, det_err_csv='{errCsv}' where id = {detId}";

            bool dbOk = ExecuteNonSql(pgConnection, logProgName, moduleName, jobId, rowNum, sql);

            if (dbOk)
            {
                dbOk = AddAction(pgConnection, pgSchema, logProgName, moduleName, jobId, rowNum, detId, actionDone);
            }
            return dbOk;
        }

        public static bool IsActionDone(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId, int rowNum
            , int detId, string detAction)
        {
            bool actDone = false;
            string sql = $"select * from {pgSchema}.filedetail_actions where filedet_id = {detId} and action_done ='{detAction}'";
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
                            actDone = true;
                            break;
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                LogSqlError(moduleName, logProgName, "GetMappedVal", jobId, rowNum, sql, ex);
                throw;
            }
            return actDone;
        }

        public static void GetCouriers(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizTypeToRead, int jobId
            , string workdirYmd, string waitingAction, string doneAction
            , List<string> courierList, bool isApy, string courierCsv, out string sql)
        {
            string wherePart = " and lower(apy_flag) = '" + (isApy ? "y" : "n") + "'";
            string[] whCouriers = courierCsv.Trim().Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (string.IsNullOrEmpty(courierCsv)==false)
            {
                wherePart += " and courier_id in ('" +string.Join("','", whCouriers) + "')";
            }

            sql = "select distinct courier_id"+
                $" from {pgSchema}.filedetails" +
                $" join {pgSchema}.fileinfo on fileinfo.id = filedetails.fileinfo_id" +
                $" where fileinfo.isdeleted='0'" +
                $" and fileinfo.module_name = '{moduleName}'" +
                $" and fileinfo.biztype = '{bizTypeToRead}'" +
                $" and fileinfo.fpath like '%\\\\{workdirYmd}\\\\%'" +
                wherePart;

            if (string.IsNullOrEmpty(doneAction)==false)
            {
                sql += " and exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{doneAction}')";
            }
            if (string.IsNullOrEmpty(waitingAction)==false)
            {
                sql += " and not exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{waitingAction}')";
            }

            DataSet ds = GetDataSet(pgConnection, logProgName, moduleName, jobId, sql);

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach(DataRow dr in ds.Tables[0].Rows)
                {
                    courierList.Add(Convert.ToString(dr[0]));
                }
            }
        }
        public static DataSet GetLetterCourier(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizTypeToRead, int jobId
            , string workdirYmd, string waitingAction, string doneAction
            , string courierId, bool isApy, string colSelection, string orderBy, out string sql)
        {
            string wherePart = " and lower(apy_flag) = '" + (isApy ? "y" : "n") + "'";

            sql = $"select {colSelection} "+
                $" from {pgSchema}.filedetails" +
                $" join {pgSchema}.fileinfo on fileinfo.id = filedetails.fileinfo_id" +
                $" where fileinfo.isdeleted='0'" +
                $" and fileinfo.module_name = '{moduleName}'" +
                $" and fileinfo.biztype = '{bizTypeToRead}'" +
                $" and courier_id='{courierId}'" +
                $" and fileinfo.fpath like '%\\\\{workdirYmd}\\\\%'" +
                wherePart;

            if (string.IsNullOrEmpty(doneAction)==false)
            {
                sql += " and exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{doneAction}')";
            }
            if (string.IsNullOrEmpty(waitingAction)==false)
            {
                sql += " and not exists" +
                $" (select 1 from {pgSchema}.filedetail_actions fa where fa.filedet_id = filedetails.id and" +
                $"   action_void = '0' and action_done='{waitingAction}')";
            }

            sql += $" order by {orderBy}";

            DataSet ds = GetDataSet(pgConnection, logProgName, moduleName, jobId, sql);

            return ds;
        }

    }
}
