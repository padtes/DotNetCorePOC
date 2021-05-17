using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;

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
                Logger.WriteInfo(moduleName, "ReadCSV", "No Read job found");
                return true;
            }
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

            if (FileIsValid(connectionStr, pgSchema, aJob))
            {
                WriteRawData(connectionStr, pgSchema, aJob);
            }
        }

        private void WriteRawData(string connectionStr, string pgSchema, ReadJob aJob)
        {
            throw new NotImplementedException();
        }

        private bool FileIsValid(string connectionStr, string pgSchema, ReadJob aJob)
        {
            //file still exists
            string fullFileName = aJob.OutputPath.TrimEnd('/') + "/" + aJob.OutFileName;

            if (File.Exists(fullFileName) == false)
            {
                //to do mark the job as Fail2- file missing
                Logger.Write(moduleName, "FileIsValid", $"{fullFileName} file missing", Logger.ERROR);
                return false;
            }

            ReadTemplateHeader hdr = new ReadTemplateHeader();
            bool templateOk = LoadFileTemplateFromDB(connectionStr, pgSchema, aJob, hdr);
            if (templateOk == false)
            {
                //to do mark the job as Fail3- template missing
                Logger.Write(moduleName, "FileIsValid", $"Client: {aJob.ClientId} / {aJob.FileDefId} file template missing", Logger.ERROR);
                return false;
            }


            return true;
        }

        private bool LoadFileTemplateFromDB(string connectionStr, string pgSchema, ReadJob aJob, ReadTemplateHeader hdr)
        {
            string sql = $"select id, output_column, input_index from {pgSchema}.read_template_det where template_id={aJob.FileDefId} and coalesce(output_column, '') <> '' order by input_index,output_column;";
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
                                }
                                );
                            }
                        }
                    }

                    conn.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write(moduleName, "LoadReadJobs", "Failed sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "LoadReadJobs", ex);
            }
            return false;
        }

        public bool DownloadFile(string pgConnectionStr, string pgSchema, ReadJob aJob)
        {
            try
            {
                //to do 
                //download file
                //
                string nowStr = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
                aJob.OutFileName = $"{aJob.ClientId}_{nowStr}.csv";
                //to do 
                //change status = downloaded
                //write to DB
                Logger.WriteInfo(moduleName, "DownloadFile", $"{aJob.Id} downloaded URL: {aJob.Url}");

                return true;
            }
            catch (Exception ex)
            {
                //to do mark the job as Fail1- Download file failed
                Logger.Write(moduleName, "DownloadFile", $"{aJob.Id} Failed URL: {aJob.Url}", Logger.WARNING);
                Logger.WriteEx(moduleName, "DownloadFile", ex);
                return false;
            }
        }

        private static void LoadReadJobs(string connectionStr, string pgSchema, List<ReadJob> readJobs)
        {
            //string postgresDBConnStr = "Server=localhost; Port=5433; Database=postgres; User Id=postgres; Password=super_post_pwd; ";
            //string postgresDBConnStr = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simple_user_pwd; ";
            string sql = $"select id, client_id, file_def_id, url, output_path from {pgSchema}.read_job where status='pending' order by id;";
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
                                    FileDefId = rdr.GetInt32(2),
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
                Logger.Write(moduleName, "LoadReadJobs", "Failed sql:" + sql, Logger.WARNING);
                Logger.WriteEx(moduleName, "LoadReadJobs", ex);
            }
        }
    }
}
