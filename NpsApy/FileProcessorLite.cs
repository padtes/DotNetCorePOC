using Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace NpsApy
{
    internal class FileProcessorLite : FileProcessor
    {
        private const string moduleName = "FileProcLite";

        public FileProcessorLite(string schemaName, string connectionStr) : base(schemaName, connectionStr)
        {

        }
        public override string GetBizType()
        {
            return BIZ_LITE;
        }

        public override bool ProcessBiz(string operation, string runFor, string courierCsv)
        {
            staticParamList = new List<string>() { "output_par", "output_lite", "output_apy" };
            LoadParam(BIZ_LITE);
            ValidateStaticParam(BIZ_LITE);

            return base.ProcessBiz(operation, runFor, courierCsv);
        }

        public  override void ProcessInput(string runFor)
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

            //for each sub directory  -- this can go in parallel, not worth it - mostly 1 date at a time
            for (int i = 0; i < dateDirectories.Count; i++)
            {
                // collect file names to process - make entry in File Header table with status = "TO DO"
                CollectFilesNpsLiteApyDir(dateDirectories[i], reprocess: (runFor != "all"));
            }
            //------ 2 -----------
            //read json file Def for lite apy in systemDir
            //configLiteApyDef = json deserialize 

            //Process files  -- this can go in parallel
            //read File Header table with status = "TO DO"
            //parallel process - pass file Id as param
            //
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
            CreateWorkDir(dateAsDir);
            //---------------------- 1 ---------------- 
            // for each file in dir:dateAsDir under 

            //if record not found for the dateAsDir / file name - insert
            //save all details as full input and work path - Copy file from input to work

            //if reprocess == true and record found - update status = TO DO and dateTime of status update, overwrite file from input to work
            //if reprocess == false and record found - ignore
        }

        private void CreateWorkDir(string dateAsDir)
        {
            string curWorkDirForDt = workDir + "/" + dateAsDir;
            if (Directory.Exists(curWorkDirForDt) == false)
                Directory.CreateDirectory(curWorkDirForDt);

            string outParentDir = paramsDict["output_par"];
            curWorkDirForDt = curWorkDirForDt + "/" + outParentDir;
            if (Directory.Exists(curWorkDirForDt) == false)
                Directory.CreateDirectory(curWorkDirForDt);

            string tmpOut = curWorkDirForDt + "/" + paramsDict["output_apy"];
            if (Directory.Exists(tmpOut) == false)
                Directory.CreateDirectory(tmpOut);

            tmpOut = curWorkDirForDt + "/" + paramsDict["output_lite"];
            if (Directory.Exists(tmpOut) == false)
                Directory.CreateDirectory(tmpOut);
        }

        internal void ProcessLiteApyFile(int fileID)
        {
            //update header as WIP - dateTime of status update
            //copy file to work dir
            //use config defining input file structre loaded before calling this in loop
            //Save from txt file to data table
            //save photos and signatures
            //delete file from input dir
        }

    }

}