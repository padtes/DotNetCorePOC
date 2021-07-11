using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace DataProcessor
{
    public class FileProcessorLite : FileProcessor
    {
        private const string logProgName = "FileProcLite";
        public FileProcessorLite(string connectionStr, string schemaName, string opName) : base(connectionStr, schemaName, opName)
        {

        }
        public override string GetModuleName()
        {
            return ConstantBag.MODULE_LITE;
        }

        protected override void LoadModuleParam(string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { ConstantBag.PARAM_OUTPUT_PARENT_DIR, ConstantBag.PARAM_OUTPUT_LITE_DIR, ConstantBag.PARAM_OUTPUT_APY_DIR, ConstantBag.PARAM_IMAGE_LIMIT };

            //read details based on date from system param table
            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, logProgName, GetModuleName(), jobId
                , out systemConfigDir, out inputRootDir, out workDir);

            ProcessorUtil.ValidateStaticParam(GetModuleName(), ConstantBag.MODULE_LITE, logProgName, paramsDict, staticParamList);
        }

        public override void ProcessInput(string runFor)
        {
            /*
            INPUT directory structure
            --NPS LITE AND APY

            inputRootDir / ddmmyyyy / nps_lite_apy --- input files are here  
            for ex. c:/pranProj / UserFolder / 21052021 / nps_lite_apy / PTGCHG0515202114150521001.txt | PTGPRN0515202114150521000.txt  etc.

            OUTPUT file structure 
            --NPS LITE
                workDir / ddmmyyyy / nps_lite_apy / nps 
                workDir / ddmmyyyy / nps_lite_apy / nps / <status file> <response file> 
                workDir / ddmmyyyy / nps_lite_apy / nps / courier_name_ddmmyy / <photo_01..999> <sig_01..999>
            */
            List<string> dateDirectories = new List<string>();

            if (runFor == "all" || runFor == "allover")  //All unprocessed - new Or all - inclluding partially processed
            {
                //scan base directory
                dateDirectories.AddRange(Directory.GetDirectories(inputRootDir));
            }
            else
            {
                if (File.Exists(inputRootDir + "/" + runFor))
                {
                    //Process files from  base directory/runFor sub dir only
                    dateDirectories.Add(runFor);
                }
            }

            FileTypeMaster fTypeMaster = GetFileTypeMaster();
            if (fTypeMaster == null)
            {
                Logger.Write(logProgName, "ProcessInput", 0
                    , $"NO FileTypeMaster for {ConstantBag.LITE_IN} module:{GetModuleName()} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}"
                    , Logger.ERROR);

                return; //----------------------------
            }
            string jsonFName = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            if (File.Exists(jsonFName) == false)
            {
                Logger.Write(logProgName, "ProcessInput", 0
                    , $"FILE NOT FOUND: {jsonFName}. Aborting. Check Filemaster  {ConstantBag.LITE_IN} for file name"
                    , Logger.ERROR);
                return; //----------------------------
            }

            string curWorkDir;
            bool reprocess = (runFor != "all");

            //for each sub directory  -- this can go in parallel, not worth it - mostly 1 date at a time
            for (int i = 0; i < dateDirectories.Count; i++)
            {
                // collect file names to process - make entry in File Header table with status = "TO DO"
                //to do use fileTypeMaster - to see file name pattern to read files
                CollectFilesNpsLiteApyDir(dateDirectories[i], reprocess, out curWorkDir);

                SaveToDb(dateDirectories[i], fTypeMaster, reprocess);
            }
        }

        private void SaveToDb(string dateAsDir, FileTypeMaster fTypeMaster, bool reprocess)
        {
            List<FileInfoStruct> listFiles = new List<FileInfoStruct>();

            //read File Header table with status = "TO DO" OR in any Work in Progress from last failed job
            //:: *********************************
            //:: CAUTION - if there is job running as different instance or from different machine
            //:: - it can pick actual Work in Progress from that one causing double processing
            //::-- in that case will need lock / unlock and reset mechanism
            //:: *********************************
            string[] statusToWork = new[] { ConstantBag.FILE_LC_STEP_TODO, ConstantBag.FILE_LC_WIP };

            //assuming input will be directly under yyyymmdd / npsLite_apy directory 
            string inpFilesDir = dateAsDir + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR];

            DbUtil.GetFileInfoList(pgConnection, pgSchema, logProgName, GetModuleName(), jobId, listFiles, inpFilesDir, statusToWork);

            //LOGGING - cannot be file based - multiple threads cannot use the same file safely
            Logger.StopFileLog();
            try
            {
                //Process files  -- this can go in parallel  :: TO DO

                foreach (FileInfoStruct inFile in listFiles)
                {
                    ProcessLiteApyFile(inFile, fTypeMaster, dateAsDir);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgName, "SaveToDb", jobId, ex);
            }
            Logger.StartFileLog();
        }

        private FileTypeMaster GetFileTypeMaster()
        {
            return DbUtil.GetFileTypeMaster(pgConnection, pgSchema, GetModuleName(), ConstantBag.LITE_IN, jobId);
        }

        internal void CollectFilesNpsLiteApyDir(string dateAsDir, bool reprocess, out string curWorkDir)
        {
            Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"Directory started: {dateAsDir}");

            string apyOutDir, liteOutDir;
            CreateWorkDir(dateAsDir, out apyOutDir, out liteOutDir, out curWorkDir);

            Console.WriteLine($"{apyOutDir} - {liteOutDir} - {curWorkDir}");

            //assuming input will be directly under yyyymmdd / npsLite_apy directory 
            string inpFilesDir = dateAsDir + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR];
            string[] curFileList = Directory.GetFiles(inpFilesDir);

            Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"Directory : {inpFilesDir} has {curFileList.Length} files");

            // for each file in dir:dateAsDir under  date/npsLite_apy
            for (int i = 0; i < curFileList.Length; i++)
            {
                string aFile = curFileList[i];
                string fName = Path.GetFileName(aFile);
                // string workDest = curWorkDir + "\\" + fName;

                //if (File.Exists(workDest) == false || reprocess)   //we do NOT COPY files from in to work
                //{
                //    File.Copy(aFile, workDest, reprocess);
                //}

                //save all details as full input and work path
                FileInfoStruct fInfo = new FileInfoStruct()
                {
                    fname = fName,
                    fpath = inpFilesDir,
                    isDeleted = false,
                    bizType = ConstantBag.LITE_IN,
                    moduleName = GetModuleName(),
                    direction = ConstantBag.DIRECTION_IN,
                    addedDate = DateTime.Now, //.ToString("yyyy/MM/dd HH:mm:ss"),
                    addedBy = ConstantBag.BATCH_USER,
                    updateDate = DateTime.Now, //.ToString("yyyy/MM/dd HH:mm:ss"),
                    updatedBy = ConstantBag.BATCH_USER,
                    inpRecStatus = ConstantBag.FILE_LC_STEP_TODO,
                    inpRecStatusDtUTC = DateTime.UtcNow,
                    // TO BE DONE  --need to save a record NpgSql odd
                    importedFrom = "TBD",
                    courierSname = "",
                    courierMode = "",
                    nprodRecords = 0,
                    archiveAfter = 0,
                    archivePath = "TBD",
                    purgeAfter = 0,
                    addedfromIP = "localhost",
                    updatedFromIP = "localhost"
                };

                DbUtil.UpsertFileInfo(pgConnection, pgSchema, logProgName, GetModuleName(), jobId, i, reprocess, fInfo, out string actionTaken);

                Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"file #{i} - {actionTaken} : {aFile}");
            }

            Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"Directory DONE: {dateAsDir}");
        }

        private void CreateWorkDir(string dateAsPath, out string apyOutDir, out string liteOutDir, out string curWorkDir)
        {
            string[] pathParts = dateAsPath.Split(new char[] { '/', '\\' });
            string dateAsDir = pathParts[pathParts.Length - 1];

            string curWorkDirForDt = workDir + "/" + dateAsDir;
            if (Directory.Exists(curWorkDirForDt) == false)
                Directory.CreateDirectory(curWorkDirForDt);

            string outParentDir = paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR];
            string tmpOut = curWorkDirForDt + "/" + outParentDir;
            if (Directory.Exists(tmpOut) == false)
                Directory.CreateDirectory(tmpOut);

            liteOutDir = tmpOut + "/" + paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];
            if (Directory.Exists(liteOutDir) == false)
                Directory.CreateDirectory(liteOutDir);

            apyOutDir = tmpOut + "/" + paramsDict[ConstantBag.PARAM_OUTPUT_APY_DIR];
            if (Directory.Exists(apyOutDir) == false)
                Directory.CreateDirectory(apyOutDir);

            curWorkDir = dateAsPath; //we do NOT COPY files from in to work
                                     //curWorkDir = tmpOut + "/" + paramsDict["output_duplicate"]; // to keep copy of input files
                                     //if (Directory.Exists(curWorkDir) == false)
                                     //    Directory.CreateDirectory(curWorkDir);

        }

        internal void ProcessLiteApyFile(FileInfoStruct inpFileInfo, FileTypeMaster fTypeMaster, string dateAsDir)
        {
            string tmpSql = "";
            //update header as WIP - dateTime of status update
            inpFileInfo.inpRecStatus = ConstantBag.FILE_LC_WIP;
            DbUtil.UpdateFileInfoStatus(pgConnection, pgSchema, inpFileInfo, ref tmpSql);

            //paramsDict
            string tmpFName = inpFileInfo.fpath + "\\" + inpFileInfo.fname;
            string tmpJsonFName = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;

            //Save from txt file to data table
            ////save photos and signatures
            bool suc = FileProcessorUtil.SaveInputToDB(this, inpFileInfo, jobId, tmpFName, tmpJsonFName, paramsDict, dateAsDir);

            if (suc)
            {
                inpFileInfo.inpRecStatus = ConstantBag.FILE_LC_STEP_TO_DB;
                DbUtil.UpdateFileInfoStatus(pgConnection, pgSchema, inpFileInfo, ref tmpSql);

                //delete file from input dir ???
                //TO DO
                File.Move(tmpFName, @"c:\zunk\deleted_files\" + inpFileInfo.fname);
            }
            else
            {
                inpFileInfo.inpRecStatus = ConstantBag.FILE_LC_STEP_ERR1;
                DbUtil.UpdateFileInfoStatus(pgConnection, pgSchema, inpFileInfo, ref tmpSql);
            }
        }

        public override string GetBizTypeImageDirName(InputRecordAbs inputRecord)
        {
            if (inputRecord == null || inputRecord is InputHeader)
            {
                return "";
            }

            string flag = inputRecord.GetColumnValue("apy_flag");   //to do ----- find what column to use -What Value to use

            if (string.IsNullOrEmpty(flag) == false && inputRecord.GetColumnValue("apy_flag").ToLower() != "y")
                return paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];

            return "";
        }

        public override ReportProcessor GetReportProcessor(string operation)
        {
            return new ReportProcessorLite(GetConnection(), GetSchema(), GetModuleName(), operation);
        }
    }

}