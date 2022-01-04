using Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace DeletePRAN
{
    class ProgramDeletePran
    {
        private const string logProgName = "DeletePran";
        static void Main(string[] args)
        {
            Console.WriteLine("Delete PRAN prior to... daily job started");

            string pgSchema, pgConnection, logFileName, dirForTrash, printedOkCode, workDir, inputRoot;
            int numberOfDays;

            // read appSettings.json config
            ReadSystemConfig(out pgSchema, out pgConnection, out logFileName
                , out dirForTrash, out numberOfDays, out printedOkCode, out workDir, out inputRoot);
            Logger.SetLogFileName(logFileName);

            DateTime deleteBefDate = DateTime.Now.AddDays(-1 * numberOfDays);
            Logger.WriteInfo(logProgName, "main", 0, "Delete Files before date " + deleteBefDate.ToString("dd-MMM-yyyy"));

            string trialFinal = "trial";
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "final")
                    trialFinal = args[0].ToLower();
            }

            Logger.WriteInfo(logProgName, "main", 0, $"run is {trialFinal}. This is runtime arg final or trial");

            ProcessPurge(pgSchema, pgConnection, printedOkCode, numberOfDays, trialFinal, dirForTrash, workDir, inputRoot);

            Logger.WriteInfo(logProgName, "main", 0, $"run is {trialFinal}. This is runtime arg final or trial");
        }

        private static void ProcessPurge(string pgSchema, string pgConnection, string printedOkCode, int numberOfDays, string trialFinal
            , string dirForTrash, string workDir, string inputRootDir)
        {
            DataSet ds = null;
            string sql = $"select * from {pgSchema}.get_data_purge_counts('t', {numberOfDays}, '{printedOkCode}');";

            try
            {
                ds = DbUtil.GetDsForSql(pgConnection, sql);
            }
            catch (Exception ex)
            {
                Logger.Write(logProgName, "deleteMain", 0, $"Failed to get/delete {sql}. see ex below", Logger.ERROR);
                Logger.WriteEx(logProgName, "deleteMain", 0, ex);
            }
            if (ds == null || ds.Tables.Count < 1 || ds.Tables[0].Rows.Count < 1)
            {
                Logger.Write(logProgName, "deleteMain", 0, $"NO DATA FOUND {sql}. Stopping", Logger.WARNING);
                return;
            }

            Logger.WriteInfo(logProgName, "deleteMain", 0, $"Going to delete following:");
            Logger.WriteInfo(logProgName, "deleteLoop", 0, "id\tDate-Dir\tFile Name\tAdded Date\tTotal to delete\tTotal Det Count");
            bool fileDelErr = false;

            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                int id = Convert.ToInt32(ds.Tables[0].Rows[i]["id"]);
                string dtDir = Convert.ToString(ds.Tables[0].Rows[i]["fpath"]);
                dtDir = dtDir.Substring(inputRootDir.Length);
                string[] dtSplit = dtDir.Split(new char[] { '\\', '/' });
                dtDir = dtSplit[0];

                string fname = Convert.ToString(ds.Tables[0].Rows[i]["fname"]);
                string addedDt = Convert.ToString(ds.Tables[0].Rows[i]["addeddate"]);
                int detCountDelete = Convert.ToInt32(ds.Tables[0].Rows[i]["det_count_delete"]);
                int detCountTot = Convert.ToInt32(ds.Tables[0].Rows[i]["det_count_tot"]);

                Logger.WriteInfo(logProgName, "deleteLoop", 0, $"{id}\t{dtDir}\t{fname}\t{addedDt}\t{detCountDelete}\t{detCountTot}");

                string workSubDir = Path.Join(workDir, dtDir);
                string[] fileNames = Directory.GetFiles(workSubDir, "*.jpg", SearchOption.AllDirectories);  //all signs and photos including on-hold
                List<string> workSubDirs = new List<string>();
                LoadImageSubDirs(fileNames, workSubDirs);

                List<string> onHoldFiles = new List<string>();
                LoadOnHoldFileNames(pgConnection, printedOkCode, id, onHoldFiles);

                string trashSubDir = Path.Join(dirForTrash, dtDir);
                Logger.WriteInfo(logProgName, "deleteLoop", 0, $"{id}\tTrash: {trashSubDir}");
                foreach (string imgSubDir in workSubDirs)
                {
                    Logger.WriteInfo(logProgName, "deleteLoop", 0, $"{id}\twork: {imgSubDir}");
                }

                if (trialFinal == "final")
                {
                    try
                    {
                        DeleteFiles(fileNames, workSubDirs, onHoldFiles, trashSubDir);
                    }
                    catch(Exception ex)
                    {
                        Logger.Write(logProgName, "deleteLoop", 0, $"Final Failed to delete files {dtDir}. see ex below", Logger.ERROR);
                        Logger.WriteEx(logProgName, "deleteLoop", 0, ex);
                        fileDelErr = true;
                    }
                }
            }
            if (trialFinal == "final" && fileDelErr == false)
            {
                sql = $"select * from {pgSchema}.get_data_purge_counts('f', {numberOfDays}, '{printedOkCode}');";
                try
                {
                    ds = DbUtil.GetDsForSql(pgConnection, sql);
                }
                catch (Exception ex)
                {
                    Logger.Write(logProgName, "deleteMain", 0, $"Final Failed to get/delete {sql}. see ex below", Logger.ERROR);
                    Logger.WriteEx(logProgName, "deleteMain", 0, ex);
                }
            }
            else
            {
                Logger.Write(logProgName, "deleteMain", 0, $"Skipped Delete Rec {trialFinal} / {fileDelErr}.", Logger.WARNING);
            }
        }

        private static void DeleteFiles(string[] fileNames, List<string> workSubDirs, List<string> onHoldFiles, string trashSubDir)
        {
            //delete sign + photo
            foreach (string imgFile in fileNames)
            {
                if (onHoldFiles.Contains(imgFile) == false)
                {
                    File.Delete(imgFile);
                }
            }

            DeleteDirIfEmpty(workSubDirs);
            //?? letters - there are multiple letters in a file - see if need to delete ?? Manual?

            //delete files from trash
            if (Directory.Exists(trashSubDir))
            {
                Directory.Delete(trashSubDir, true);
            }
        }

        private static void LoadImageSubDirs(string[] fileNames, List<string> workSubDirs)
        {
            foreach (string imgFile in fileNames)
            {
                string lastFolderName = Path.GetDirectoryName(imgFile);
                if (workSubDirs.Contains(lastFolderName) == false)
                {
                    workSubDirs.Add(lastFolderName);
                }
            }
        }

        private static void LoadOnHoldFileNames(string pgConnection, string printedOkCode, int id, List<string> onHoldFiles)
        {
            string detFiles = $"select files_saved->0->>'actual_file_path' f1, files_saved->1->>'actual_file_path' f2 " +
                $" from ventura.filedetails d where d.fileinfo_id = {id} and det_err_csv <> '{printedOkCode}';";
            DataSet dsDetFiles = DbUtil.GetDsForSql(pgConnection, detFiles);
            if (dsDetFiles != null && dsDetFiles.Tables.Count > 0)
            {
                foreach (DataRow dr in dsDetFiles.Tables[0].Rows)
                {
                    AddFileNameOnHold(onHoldFiles, dr, "f1");
                    AddFileNameOnHold(onHoldFiles, dr, "f2");
                }
            }
        }

        private static void DeleteDirIfEmpty(List<string> workSubDirs)
        {
            foreach (string subDir in workSubDirs)
            {
                try
                {
                    Directory.Delete(subDir);
                }
                catch (Exception)
                {
                    //do nothing - dir is not empty
                }
            }
        }

        private static void AddFileNameOnHold(List<string> onHoldFiles, DataRow dr, string fileCol)
        {
            if (dr[fileCol] != DBNull.Value)
            {
                string fn = Convert.ToString(dr[fileCol]);
                onHoldFiles.Add(fn);
            }
        }

        private static void ReadSystemConfig(out string pgSchema, out string pgConnection, out string logFileName
            , out string dirForTrash, out int numberOfDays, out string printedOkCode, out string workDir, out string inputRoot)
        {
            numberOfDays = 90;

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            logFileName = configuration["logFileName"];
            if (string.IsNullOrEmpty(logFileName))
                throw new Exception("logFileNm Not in appSettings.json. ABORTING");
            Logger.SetLogFileName(logFileName);

            pgSchema = configuration["pgSchema"];
            ShowError(pgSchema, "pgSchema");

            pgConnection = configuration["pgConnection"];
            ShowError(pgConnection, "pgConnection");
            if (DbUtil.CanConnectToDB(pgConnection) == false)
                throw new Exception("pgConnection Not in able to Connect. ABORTING");

            dirForTrash = configuration["dirForTrash"];
            ShowError(dirForTrash, "dirForTrash");
            if (Directory.Exists(dirForTrash) == false)
            {
                Logger.Write(logProgName, "ReadSystemConfig", 0, $"directory Not Found {dirForTrash} to delete files. ABORTING", Logger.ERROR);
                throw new Exception($"directory Not Found {dirForTrash} to delete files. ABORTING");
            }

            string sNumberOfDays = configuration["numberOfDays"];
            ShowError(sNumberOfDays, "numberOfDays");
            if (int.TryParse(sNumberOfDays, out numberOfDays) == false)
            {
                Logger.Write(logProgName, "ReadSystemConfig", 0, "numberOfDays Not NUMERIC in appSettings.json. Defaulting to 90 days", Logger.ERROR);
                throw new Exception($"numberOfDays Not NUMERIC in appSettings.json. ABORTING");
            }
            if (numberOfDays < 90)
            {
                Logger.Write(logProgName, "ReadSystemConfig", 0, $"numberOfDays {sNumberOfDays} less than 90 in appSettings.json. ABORTING", Logger.ERROR);
                throw new Exception($"numberOfDays Not NUMERIC in appSettings.json. ABORTING");
            }

            printedOkCode = configuration["printedOkCode"];
            ShowError(printedOkCode, "printedOkCode");

            workDir = configuration["workDir"];
            ShowError(workDir, "workDir");

            inputRoot = configuration["inputRoot"];
            ShowError(inputRoot, "inputRoot");
        }

        private static void ShowError(string paramVal, string paramNm)
        {
            if (string.IsNullOrEmpty(paramVal))
            {
                Logger.Write(logProgName, "ReadSystemConfig", 0, $"{paramNm} Not in appSettings.json. ABORTING", Logger.ERROR);
                throw new Exception($"{paramNm} Not in appSettings.json. ABORTING");
            }
        }
    }
}
