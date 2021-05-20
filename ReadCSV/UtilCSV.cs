using CsvHelper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ReadCSV
{
    public class UtilCSV
    {
        private static string moduleName = "UtilCSV";

        public bool ReadCSV(string connectionStr, string pgSchema)
        {
            List<ReadJob> readJobs = new List<ReadJob>();
            LoadReadJobs(connectionStr, pgSchema, readJobs);

            if (readJobs.Count == 0)
            {
                Logger.WriteInfo(moduleName, "ReadCSV", 0, "No Read job found");
                return true;
            }
            
            Logger.WriteInfo(moduleName, "ProcessFile", 0, $"Count of Read jobs:{readJobs.Count}");

            foreach (ReadJob aJob in readJobs)
            {
                if (DownloadFile(connectionStr, pgSchema, aJob))
                {
                    ProcessFile(connectionStr, pgSchema, aJob);
                }
            }
            return true;
        }

        public void ProcessFile(string connectionStr, string pgSchema, ReadJob aJob)
        {
            Logger.WriteInfo(moduleName, "ProcessFile", aJob.Id, $"Started Read job file {aJob.OutFileName}");
            if (IsFileValid(connectionStr, pgSchema, aJob))
            {
                WriteRawData(connectionStr, pgSchema, aJob);

                UpdateStat(connectionStr, pgSchema, ReadJob.RAW_WRITTEN, aJob.Id, 0); //change status = readToRaw
                Logger.WriteInfo(moduleName, "ProcessFile", aJob.Id, $"Done Read job file {aJob.OutFileName}");
                //to do
                //define table to save Stored Proc to call for custom validation
                //link to job
                //call SP, with Job ID as param
            }
            else
            {
                //status = error is done by isFileValid because it has details
                Logger.Write(moduleName, "ProcessFile", aJob.Id, $"Error in file. See above. Read file {aJob.OutFileName}", Logger.ERROR);
            }
        }

        private void WriteRawData(string connectionStr, string pgSchema, ReadJob aJob)
        {
            Logger.WriteInfo(moduleName, "WriteRawData", aJob.Id, $"Started WriteRawData for job file {aJob.OutFileName}");

            ReadTemplateHeader templateHdr = new ReadTemplateHeader();
            LoadFileTemplateFromDB(connectionStr, pgSchema, aJob, templateHdr);

            //to do : Delete from Raw Data where JobID = aJob.Id -that will allow re-run of same job
            //to do cannnot delete if print was done for these

            string sqlInsPart1 = GetInsertPart1(pgSchema, templateHdr);
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlInsPart1);

            string fullFileName = aJob.OutputPath.TrimEnd('/') + "/" + aJob.OutFileName;

            int rowNum = 0;
            int errCount = 0;
            int breakCnt = 0;

            using (var reader = new StreamReader(fullFileName))
            using (var csvRdr = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csvRdr.Read();
                csvRdr.ReadHeader();
                while (csvRdr.Read())
                {
                    rowNum++;
                    if (IsRowValid(csvRdr, aJob.Id, rowNum, templateHdr.ColumnList, ref errCount))
                    {
                        GetInsertPart2(sb, aJob.Id, rowNum, templateHdr.ColumnList, csvRdr, breakCnt == 0);
                        breakCnt++;
                        if (breakCnt > 30)
                        {
                            breakCnt = 0;
                            ExecuteInsert(connectionStr, sb.ToString(), aJob.Id, rowNum);
                            sb.Clear();
                            sb.Append(sqlInsPart1);
                        }
                    }
                }
            }

            if (breakCnt > 0) //has some valid rows
            {
                ExecuteInsert(connectionStr, sb.ToString(), aJob.Id, rowNum);
            }

            Logger.Write(moduleName, "WriteRawData", aJob.Id, "Total Number of rows " + rowNum + ", error count:" + errCount, (errCount == 0 ? Logger.INFO : Logger.WARNING));

            //using(NpgsqlConnection conn = new NpgsqlConnection(connectionStr))
            //{
            //    using (var writer = conn.BeginBinaryImport("copy user_data.part_list from STDIN (FORMAT BINARY)"))
            //    {
            //    }
            //}
        }

        private static void ExecuteInsert(string connectionStr, string sql, int jobId, int rowNum)
        {
            sql = sql + ";";

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionStr))
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
        }

        private static void UpdateStat(string connectionStr, string pgSchema, string stat, int jobId, int rowNum)
        {
            string nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string sql = $"update {pgSchema}.read_job set status='{stat}', status_row_num='{rowNum}', last_upd_date='{nowStr}' where id = {jobId};";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionStr))
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
                Logger.Write(moduleName, "UpdateStat", jobId, $"Error Updating Stat @row {rowNum}, see sql below", Logger.WARNING);
                Logger.Write(moduleName, "UpdateStat", jobId, "Error Sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "UpdateStat", jobId, ex);
                throw;
            }
        }
        private void GetInsertPart2(StringBuilder sb, int jobId, int rowNum, List<ReadTemplateDet> columnList, CsvReader csvRdr, bool isFirst)
        {
            if (isFirst == false)
            {
                sb.Append(',');
            }

            sb.Append('(').Append(jobId).Append(',').Append(rowNum);

            for (int i = 0; i < columnList.Count; i++)
            {
                string cell = csvRdr[columnList[i].InputIndex];
                
                sb.Append(',');
                sb.Append(columnList[i].GetFormattedValue(cell));
            }
            sb.Append(')');
        }

        private bool IsRowValid(CsvReader csvRdr, int jobId, int rowNum, List<ReadTemplateDet> columnList, ref int errCount)
        {
            StringBuilder sbEr = new StringBuilder();
            sbEr.Append("Job:").Append(jobId).Append(" Row:").Append(rowNum);

            bool isValid = true;
            for (int i = 0; i < columnList.Count; i++)
            {
                string cell = csvRdr[columnList[i].InputIndex];
                if (columnList[i].IsValid(cell, sbEr) == false)
                {
                    isValid = false;
                }
            }

            if (isValid== false)
            {
                errCount++;
                //to do add this row to error log for the Job
                Logger.Write(moduleName, "IsRowValid", jobId, sbEr.ToString(), Logger.ERROR);
            }

            return isValid;
        }

        private string GetInsertPart1(string pgSchema, ReadTemplateHeader templateHdr)
        {
            string sql = $"insert into {pgSchema}.main_data (job_id, row_number";
            for (int i = 0; i < templateHdr.ColumnList.Count; i++)
            {
                sql += ", " + templateHdr.ColumnList[i].OutputColumn;
            }
            sql += $") values ";
            return sql;
        }

        private bool IsFileValid(string connectionStr, string pgSchema, ReadJob aJob)
        {
            string fullFileName = aJob.OutputPath.TrimEnd('/') + "/" + aJob.OutFileName;

            //file still exists
            if (File.Exists(fullFileName) == false)
            {
                UpdateStat(connectionStr, pgSchema, ReadJob.ERR_FILE_MISSING, aJob.Id, 0); //mark the job as Fail- file missing
                Logger.Write(moduleName, "FileIsValid", aJob.Id, $"file {fullFileName} missing", Logger.ERROR);
                return false;
            }

            ReadTemplateHeader templateHdr = new ReadTemplateHeader();
            bool templateOk = LoadFileTemplateFromDB(connectionStr, pgSchema, aJob, templateHdr);
            if (templateOk == false)
            {
                UpdateStat(connectionStr, pgSchema, ReadJob.ERR_TMPL_MISSING, aJob.Id, 0); //mark the job as Fail- template missing or err reading template
                Logger.Write(moduleName, "FileIsValid", aJob.Id, $"Client: {aJob.ClientId} / {aJob.ReadTemplateId} file template missing", Logger.ERROR);
                return false;
            }

            string hdrMismatch;
            bool isValid = IsHeaderMatching(fullFileName, templateHdr, out hdrMismatch);

            if (isValid == false)
            {
                UpdateStat(connectionStr, pgSchema, ReadJob.ERR_HDR_MIS, aJob.Id, 0); //mark the job as Fail- template headers mismatch
                Logger.Write(moduleName, "FileIsValid", aJob.Id, $"header Not found in pos: {hdrMismatch}", Logger.ERROR);
            }

            //record level validation not done here.
            //We could 1. validate records here and break on 1st rec OR 2. validate all records and report all errors OR 3. validate rec and insert to Raw if no error as done in WriteRawData 
            //currently we opted for 3, Client can send just corrected records
            //
            return isValid;
        }

        private static bool IsHeaderMatching(string fullFileName, ReadTemplateHeader templateHdr, out string hdrMismatch)
        {
            hdrMismatch = "";
            bool isValid = true;

            using (var reader = new StreamReader(fullFileName))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csvReader.Read();
                csvReader.ReadHeader();
                foreach (var defCol in templateHdr.ColumnList)
                {
                    if (csvReader.HeaderRecord[defCol.InputIndex].ToUpper() != defCol.InputColHeader.ToUpper())
                    {
                        hdrMismatch += "[" + defCol.InputIndex + ":" + defCol.InputColHeader + "]";
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        private bool LoadFileTemplateFromDB(string connectionStr, string pgSchema, ReadJob aJob, ReadTemplateHeader hdr)
        {
            string sql = $"select d.id, m.name OutputColumn, d.input_index, d.input_col_header, coalesce(m.data_type,'STRING') DataType, m.mandatory, m.length_or_range" +
                $" from {pgSchema}.read_template_det d" +
                $" join {pgSchema}.column_meta m on m.id = d.output_column_id" +
                $" where template_id={aJob.ReadTemplateId} and coalesce(m.name, '') <> '' order by input_index, m.name;";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                hdr.ColumnList.Add(new ReadTemplateDet()
                                {
                                    Id = rdr.GetInt32(0),
                                    OutputColumn = rdr.GetString(1),
                                    InputIndex = rdr.GetInt32(2),
                                    InputColHeader = rdr.GetString(3),
                                    DataType = rdr.GetString(4).ToUpper(),
                                    IsManadatory = rdr.GetBoolean(5),
                                    LengthRange = rdr.GetString(6),
                                }
                                );
                            }
                        }
                    }

                    conn.Close();
                }
                return (hdr.ColumnList.Count > 0);
            }
            catch (Exception ex)
            {
                Logger.Write(moduleName, "LoadReadJobs", aJob.Id, "Failed sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "LoadReadJobs", aJob.Id, ex);
            }
            return false;
        }

        public static bool DownloadFile(string connectionStr, string pgSchema, ReadJob aJob)
        {
            try
            {
                //to do 
                //download file, rename as $"{aJob.ClientId}_{nowStr}.csv"
                //

                string nowStr = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
                aJob.OutFileName = $"{aJob.ClientId}_{nowStr}.csv";

                //to do - remove placeholder File Copy from below.
                File.Copy(@"c:\zunk\Ztest.csv", aJob.OutputPath.TrimEnd('/') + "/" + aJob.OutFileName);

                UpdateStat(connectionStr, pgSchema, ReadJob.DOWNLOADED, aJob.Id, 0); //mark the job status = downloaded

                Logger.WriteInfo(moduleName, "DownloadFile", aJob.Id, $"Downloaded URL: {aJob.Url}");

                return true;
            }
            catch (Exception ex)
            {
                UpdateStat(connectionStr, pgSchema, ReadJob.ERR_DOWNLOAD, aJob.Id, 0); //mark the job as Fail- Download file failed
                Logger.Write(moduleName, "DownloadFile", aJob.Id, $"Failed URL: {aJob.Url}", Logger.WARNING);
                Logger.WriteEx(moduleName, "DownloadFile", aJob.Id, ex);
                return false;
            }
        }

        private static void LoadReadJobs(string connectionStr, string pgSchema, List<ReadJob> readJobs)
        {
            //string postgresDBConnStr = "Server=localhost; Port=5433; Database=postgres; User Id=postgres; Password=super_post_pwd; ";
            //string postgresDBConnStr = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simple_user_pwd; ";
            string sql = $"select id, client_id, read_template_id, url, output_path from {pgSchema}.read_job where status='{ReadJob.PENDING}' order by priority, id;";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                readJobs.Add(new ReadJob()
                                {
                                    Id = rdr.GetInt32(0),
                                    ClientId = rdr.GetInt32(1),
                                    ReadTemplateId = rdr.GetInt32(2),
                                    Url = rdr.GetString(3),
                                    OutputPath = rdr.GetString(4)
                                }
                                );
                            }
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Write(moduleName, "LoadReadJobs", 0, "Failed sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "LoadReadJobs", 0, ex);
            }
        }
    }
}
