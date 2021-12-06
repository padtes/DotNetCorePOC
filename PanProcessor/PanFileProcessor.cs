using CommonUtil;
using DataProcessor;
using DbOps;
using DbOps.Structs;
using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PanProcessor
{
    public class PanFileProcessor : FileProcessor
    {
        private const string logProgName = "FileProcPAN";

        public PanFileProcessor(string connectionStr, string schemaName, string operationName, string fileTypeNm) : base(connectionStr, schemaName, operationName, fileTypeNm)
        {
        }

        public override string GetBizTypeImageDirName(InputRecordAbs inputRecord)
        {
            throw new NotImplementedException();
        }

        public override string GetModuleName()
        {
            return ConstantBag.MODULE_PAN;
        }

        public override ReportProcessor GetReportProcessor()
        {
            throw new NotImplementedException();
        }

        public override void ProcessInput(string runFor, string deleteDir)
        {
            /*
            INPUT directory structure

            inputRootDir / ddmmyyyy / PAN --- input files are here  
            for ex. c:/pranProj / UserFolder / 21052021 / PAN / PRI52075501_dpr.txt | PLIeKYC00267901_Hindi.txt  etc.

            OUTPUT file structure 
            --PAN
                workDir / ddmmyyyy / PAN / indiv 
                workDir / ddmmyyyy / PAN / indiv / <status file> <response file> 
                workDir / ddmmyyyy / PAN / indiv / courier_name_ddmmyy / <photo_01..999> <sig_01..999>
                workDir / ddmmyyyy / PAN / corp 
                workDir / ddmmyyyy / PAN / corp/ <status file> <response file> 
                workDir / ddmmyyyy / PAN / corp/ courier_name_ddmmyy / <photo_01..999> <sig_01..999>
                workDir / ddmmyyyy / PAN / ekyc 
                workDir / ddmmyyyy / PAN / ekyc/ <status file> <response file> 
                workDir / ddmmyyyy / PAN / ekyc/ courier_name_ddmmyy / <photo_01..999> <sig_01..999>
            */
            List<string> dateDirectories = new List<string>();

            if (runFor == "all" || runFor == "allover")  //All unprocessed - new Or all - inclluding partially processed
            {
                //scan base directory
                dateDirectories.AddRange(Directory.GetDirectories(inputRootDir));
            }
            else
            {
                string oneDir = Path.Combine(inputRootDir, runFor);
                if (Directory.Exists(oneDir))
                {
                    //Process files from  base directory/runFor sub dir only
                    dateDirectories.Add(oneDir);
                }
            }

            FileTypeMaster fTypeMaster = DbUtil.GetFileTypeMaster(pgConnection, pgSchema, GetModuleName(), fileType, jobId);

            if (fTypeMaster == null)
            {
                Logger.Write(logProgName, "ProcessInput", 0
                    , $"NO FileTypeMaster for {fileType} module:{GetModuleName()} parameters: {runFor} system dir {systemConfigDir}, i/p dir: {inputRootDir},  work dir {workDir}"
                    , Logger.ERROR);

                return; //----------------------------
            }
            string jsonFName = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;
            if (File.Exists(jsonFName) == false)
            {
                Logger.Write(logProgName, "ProcessInput", 0
                    , $"FILE NOT FOUND: {jsonFName}. Aborting. Check Filemaster  {fileType} for file name"
                    , Logger.ERROR);
                return; //----------------------------
            }

            string curWorkDir;
            bool reprocess = (runFor != "all");

            List<string> errFiles = new List<string>();
            List<string> validPrimFiles = new List<string>();
            List<string> validOtherFiles = new List<string>();

            //for each sub directory  -- this can go in parallel, not worth it - mostly 1 date at a time
            for (int i = 0; i < dateDirectories.Count; i++)
            {
                // collect file names to process - make entry in File Header table with status = "TO DO"
                //to do use fileTypeMaster - to see file name pattern to read files
                CollectFilesPanDir(dateDirectories[i], errFiles, validPrimFiles, validOtherFiles, reprocess, out curWorkDir);

                SaveToDb(dateDirectories[i], fTypeMaster, validPrimFiles, validOtherFiles, reprocess, deleteDir);

            }
        }

        private void SaveToDb(string v, FileTypeMaster fTypeMaster, List<string> validPrimFiles, List<string> validOtherFiles, bool reprocess, string deleteDir)
        {
            //throw new NotImplementedException();
        }

        private void CollectFilesPanDir(string dateAsDir, List<string> errFiles, List<string> validPrimFiles, List<string> validOtherFiles, bool reprocess, out string curWorkDir)
        {
            Logger.WriteInfo(logProgName, "CollectFilesPanDir", jobId, $"Directory started: {dateAsDir}");
            string panOutDir;
            CreateWorkDir(dateAsDir, out panOutDir, out curWorkDir);

            string panFileGroupsCsv = paramsDict[ConstantBag.PARAM_PAN_FILE_GROUP];
            // dir command + c# regEx + rest of group file
            //for ex. if business type = eKyc then
            //   PLIeKYC*.txt + PLIeKYC([0-9]{8}).txt + PLIeKYC{{BATCH}}_Hindi.txt ,
            //   PRIeKYC*.txt + PRIeKYC([0-9]{8}).txt + PRIeKYC{{BATCH}}_Hindi.txt ,
            //   RRIeKYC*.txt + RRIeKYC([0-9]{8}).txt + RRIeKYC{{BATCH}}_Hindi.txt 
            //   if business type = Individual then PRI*.txt + PRI([0-9]{8}).txt + PRI{{BATCH}}_Hindi.txt + PRI{{BATCH}}_dpr.txt , etc, etc
            string[] tmpFileGrs = panFileGroupsCsv.Split(',');

            string inpFilesDir = dateAsDir; // for now [assumed 2021/12/05] all files are in single dir    + "\\" + paramsDict[ConstantBag.PARAM_PAN_OUTPUT_PARENT_DIR];

            foreach (string bizFileGr in tmpFileGrs)
            {
                string[] tmpGroup = bizFileGr.Split('+');
                if (tmpGroup.Length < 3)
                {
                    throw new Exception($"invalid File group {bizFileGr}");
                }

                for (int i = 0; i < tmpGroup.Length; i++)
                {
                    tmpGroup[i] = tmpGroup[i].Trim();
                }
                //get files as per dir command pattern For dir RRIeKYC*.txt or RRI*.txt
                string[] curFileList = Directory.GetFiles(inpFilesDir, tmpGroup[0]);
                string fnamePattern = tmpGroup[1];
                Regex rgx = new Regex(fnamePattern);  //RegEx to see if what is returned by dir is matching with requirement PRIeKYC([0-9]{8}).txt versus PRI([0-9]{8}).txt

                foreach (string fn in curFileList)
                {
                    CollectInTmpList(errFiles, validPrimFiles, validOtherFiles, inpFilesDir, tmpGroup, fnamePattern, rgx, fn);
                }
                int indx = 0;
                for (indx = 0; indx < validPrimFiles.Count; indx++)
                {
                    string aFile = validPrimFiles[indx];
                    string fName = Path.GetFileName(aFile);

                    SaveFileHdrRec(reprocess, inpFilesDir, indx, aFile, fName);
                }
                for (int j = 0; j < validOtherFiles.Count; j++, indx++)
                {
                    string aFile = validOtherFiles[j];
                    string fName = Path.GetFileName(aFile);

                    SaveFileHdrRec(reprocess, inpFilesDir, indx, aFile, fName);
                }

                foreach (var misDep in errFiles)
                {
                    Logger.Write(logProgName, "ProcessInput", jobId, $"Skipping Primary File missing related: {misDep}", Logger.WARNING);
                }
                errFiles.Clear(); validPrimFiles.Clear(); validOtherFiles.Clear();

                Logger.WriteInfo(logProgName, "CollectFilesPanDir", jobId, $"group DONE: {dateAsDir} -- {bizFileGr}");
            }
            Logger.WriteInfo(logProgName, "CollectFilesPanDir", jobId, $"Directory DONE: {dateAsDir}");
        }

        private void SaveFileHdrRec(bool reprocess, string inpFilesDir, int indx, string aFile, string fName)
        {
            if (string.IsNullOrEmpty(inpFilesDir))
            {
                throw new ArgumentException($"'{nameof(inpFilesDir)}' cannot be null or empty.", nameof(inpFilesDir));
            }

            if (string.IsNullOrEmpty(aFile))
            {
                throw new ArgumentException($"'{nameof(aFile)}' cannot be null or empty.", nameof(aFile));
            }

            if (string.IsNullOrEmpty(fName))
            {
                throw new ArgumentException($"'{nameof(fName)}' cannot be null or empty.", nameof(fName));
            }

            FileInfoStruct fInfo = new FileInfoStruct()
            {
                fname = fName,
                fpath = inpFilesDir,
                isDeleted = false,
                bizType = fileType,
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

            DbUtil.UpsertFileInfo(pgConnection, pgSchema, logProgName, GetModuleName(), jobId, indx, reprocess, fInfo, out string actionTaken);

            Logger.WriteInfo(logProgName, "CollectFilesPanDir", jobId, $"file #{indx} - {actionTaken} : {aFile}");
        }

        private static void CollectInTmpList(List<string> errFiles, List<string> validPrimFiles, List<string> validOtherFiles, string inpFilesDir, string[] tmpGroup, string fnamePattern, Regex rgx, string fn)
        {
            string fName = Path.GetFileName(fn);

            if (rgx.IsMatch(fName))  //this is good main file
            {
                bool relatedFileErr = false;
                var matches = Regex.Matches(fName, fnamePattern);
                string batchId = "";
                foreach (Match mt in matches)
                {
                    batchId = mt.Groups[1].Value;
                }
                if (batchId == "")
                {
                    errFiles.Add(fn);
                }
                else
                {
                    List<string> tmpOtherFiles = new List<string>();
                    for (int iGr = 2; iGr < tmpGroup.Length; iGr++)  //get rest of the files related to the main 
                    {
                        string relNam = tmpGroup[iGr].Replace("{{BATCH}}", batchId);
                        string[] curOthFileList = Directory.GetFiles(inpFilesDir, relNam);
                        if (curOthFileList.Length == 1)
                            tmpOtherFiles.Add(curOthFileList[0]);
                        else
                            relatedFileErr = true;
                    }
                    if (relatedFileErr)
                    {
                        errFiles.Add(fn);
                    }
                    else
                    {
                        validPrimFiles.Add(fn);
                        validOtherFiles.AddRange(tmpOtherFiles);
                    }
                }
            }
        }

        private void CreateWorkDir(string dateAsPath, out string panOutDir, out string curWorkDir)
        {
            string[] pathParts = dateAsPath.Split(new char[] { '/', '\\' });
            string dateAsDir = pathParts[pathParts.Length - 1];

            string curWorkDirForDt = Path.Combine(workDir, dateAsDir);// workDir + "/" + dateAsDir;
            if (Directory.Exists(curWorkDirForDt) == false)
                Directory.CreateDirectory(curWorkDirForDt);

            string outParentDir = paramsDict[ConstantBag.PARAM_PAN_OUTPUT_PARENT_DIR];
            string tmpOut = Path.Combine(curWorkDirForDt, outParentDir);
            if (Directory.Exists(tmpOut) == false)
                Directory.CreateDirectory(tmpOut);

            panOutDir = Path.Combine(tmpOut, paramsDict[ConstantBag.PARAM_PAN_OUTPUT_DIR]);
            if (Directory.Exists(panOutDir) == false)
                Directory.CreateDirectory(panOutDir);

            curWorkDir = dateAsPath; //we do NOT COPY files from in to work
                                     //curWorkDir = tmpOut + "/" + paramsDict["output_duplicate"]; // to keep copy of input files
                                     //if (Directory.Exists(curWorkDir) == false)
                                     //    Directory.CreateDirectory(curWorkDir);
        }
        protected override void LoadModuleParam(string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { ConstantBag.PARAM_PAN_OUTPUT_PARENT_DIR, ConstantBag.PARAM_PAN_OUTPUT_DIR
                , ConstantBag.PARAM_IMAGE_LIMIT, ConstantBag.PARAM_PAN_FILE_GROUP };

            //read details based on date from system param table
            paramsDict = ProcessorUtil.LoadSystemParamByBiz(pgConnection, pgSchema, logProgName, GetModuleName(), this.fileType, jobId
                , out systemConfigDir, out inputRootDir, out workDir);

            ProcessorUtil.ValidateStaticParam(GetModuleName(), fileType, logProgName, paramsDict, staticParamList);  //fileType is set to Pan_indiv / pan_corp or Pan_eKYC
        }
    }
}
