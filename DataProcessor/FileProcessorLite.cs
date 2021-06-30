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
        public FileProcessorLite(string schemaName, string connectionStr) : base(schemaName, connectionStr)
        {

        }
        public override string GetModuleName()
        {
            return ConstantBag.MODULE_LITE;
        }

        protected override void LoadModuleParam(string operation, string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { ConstantBag.PARAM_OUTPUT_PARENT_DIR, ConstantBag.PARAM_OUTPUT_LITE_DIR, ConstantBag.PARAM_OUTPUT_APY_DIR, ConstantBag.PARAM_IMAGE_LIMIT };

            LoadParam(ConstantBag.SYSTEM_PARAM);

            ValidateStaticParam(ConstantBag.MODULE_LITE);
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


            FileTypMaster fTypeMaster = GetFileTypMaster();

            //for each sub directory  -- this can go in parallel, not worth it - mostly 1 date at a time
            for (int i = 0; i < dateDirectories.Count; i++)
            {
                // collect file names to process - make entry in File Header table with status = "TO DO"
                //to do read fileTypeMaster - to see file name pattern to read files
                CollectFilesNpsLiteApyDir(dateDirectories[i], reprocess: (runFor != "all"));

                //SavetoDb(dateDirectories[i])
            }

            if (fTypeMaster == null)
            {
                Logger.WriteInfo(logProgName, "ProcessInput", 0
                    , $"NO RECORDS {ConstantBag.FILE_LC_STEP_TODO} for {GetModuleName()} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}");

                return; //----------------------------
            }


            //---------------------------- 2 ---------------
            //read json file Def for lite apy in systemDir
            //configLiteApyDef = json deserialize 

            //Process files  -- this can go in parallel
            //read File Header table with status = "TO DO"
            //string sql = $"select * from {pgSchema}.fileinfo where isdeleted='0' and biztype='{bizType}' and module_name='{moduleName}' and inp_rec_status= '{myStatus}' order by id";

            //parallel process - pass file Id as param
            //LOGGING - cannot be file based - multiple threads cannot use the same file safely
            //
        }

        private FileTypMaster GetFileTypMaster()
        {
            return DbUtil.GetFileTypMaster(pgConnection, pgSchema, GetModuleName(), ConstantBag.LITE_IN, jobId);
        }

        public override void ProcessOutput(string runFor, string courierCcsv)
        {
            ProcessNpsLiteOutput(runFor, courierCcsv);

            ProcessApyOutput(runFor, courierCcsv);
        }
        private void ProcessNpsLiteOutput(string runFor, string courierCcsv)
        {
            /*
            OUTPUT file structure 
            --NPS LITE
                --- workDir / ddmmyyyy / nps_lite_apy / nps 
                --- workDir / ddmmyyyy / nps_lite_apy / nps / <status file> <response file>
                workDir / ddmmyyyy / nps_lite_apy / nps / courier_name_ddmmyy / 
                workDir / ddmmyyyy / nps_lite_apy / nps / courier_name_ddmmyy / <PTC file> <card file> <letter files> 
             */
            //collect what all coutiers to process
            //for each courier
            //create outputs
            throw new NotImplementedException();
        }

        private void ProcessApyOutput(string runFor, string courierCcsv)
        {
            /*
            OUTPUT file structure 
            --APY
                workDir / ddmmyyyy / nps_lite_apy / apy
                workDir / ddmmyyyy / nps_lite_apy / apy / courier_name_ddmmyy / <PTC file> <card file> <letter files>
             */
            throw new NotImplementedException();
        }

        internal void CollectFilesNpsLiteApyDir(string dateAsDir, bool reprocess)
        {
            Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"Directory started: {dateAsDir}");

            string apyOutDir, liteOutDir, curWorkDir;
            CreateWorkDir(dateAsDir, out apyOutDir, out liteOutDir, out curWorkDir);

            Console.WriteLine($"{apyOutDir} - {liteOutDir} - {curWorkDir}");

            //assuming input will be directly under yyyymmdd / npsLite_apy directory 
            string inpFilesDir = dateAsDir + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR];
            string[] curFileList = Directory.GetFiles(inpFilesDir);

            // for each file in dir:dateAsDir under  date/npsLite_apy
            for (int i = 0; i < curFileList.Length; i++)
            {
                var aFile = curFileList[i];
                string fName = Path.GetFileName(aFile);
                string workDest = curWorkDir + "\\" + fName;

                //if (File.Exists(workDest) == false || reprocess)   //we do NOT COPY files from in to work
                //{
                //    File.Copy(aFile, workDest, reprocess);
                //}

                //save all details as full input and work path - Copy file from input to work
                FileInfoStruct fInfo = new FileInfoStruct()
                {
                    fname = fName,
                    fpath = curWorkDir,
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
                    purgeAfter=0,
                    addedfromIP="localhost",
                    updatedFromIP = "localhost"
                };

                DbUtil.UpsertFileInfo(pgConnection, GetModuleName(), logProgName, jobId, i, pgSchema, reprocess, fInfo, out string actionTaken);

                Logger.WriteInfo(logProgName, "CollectFilesNpsLiteApyDir", jobId, $"file #{i} - {actionTaken} : {workDest}");
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

        internal void ProcessLiteApyFile(int fileID)
        {
            //update header as WIP - dateTime of status update
            //copy file to work dir
            //use config defining input file structre loaded before calling this in loop
            //Save from txt file to data table
            ////save photos and signatures
            //delete file from input dir ???
        }

        public override string GetBizTypeDirName(InputRecordAbs inputRecord)
        {
            if (inputRecord == null || inputRecord is InputHeader)
            {
                return "";
            }

            if (inputRecord.GetColumnValue("photograph") != "")  //to do ----- find what column to use
                return paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR]; 

            return "";
        }
    }

}