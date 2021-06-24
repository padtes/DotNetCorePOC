using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace NpsApy
{
    internal class FileProcessor
    {
        private const string moduleName = "FileProcessor";

        private string pgSchema;
        private string pgConnection;

        private string systemConfigDir;
        private string inputRootDir;
        private string workDir;

        public FileProcessor(string schemaName, string connectionStr)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;
        }

        private void LoadParam(string bizType)
        {
            //read details based on date from system param table
            string sysParamStr = DbUtil.GetParamsJsonStr(pgConnection, pgSchema, bizType, "directories");
            if (sysParamStr == "")
            {
                Logger.Write(moduleName, "LoadParam.1", 0, bizType + " directory struct not in system_param table", Logger.ERROR);
                throw new Exception(bizType + " directory struct not in system_param table");
            }
            try
            {
                Dictionary<string, string> jDictCh = new Dictionary<string, string>();
                jDictCh = JsonConvert.DeserializeObject<Dictionary<string, string>>(sysParamStr);
                systemConfigDir = jDictCh["systemdir"];
                inputRootDir = jDictCh["inputdir"];
                workDir = jDictCh["workdir"];

                if (systemConfigDir == "" || inputRootDir == "" || workDir == "")
                {
                    Logger.Write(moduleName, "LoadParam.2", 0, bizType + " directory param blank", Logger.ERROR);
                    throw new Exception(bizType + " directory param blank");
                }
                systemConfigDir.TrimEnd('/');
                inputRootDir.TrimEnd('/');
                workDir.TrimEnd('/');
            }
            catch(Exception ex)
            {
                Logger.WriteEx(moduleName, "LoadParam.2", 0, ex);
                throw new Exception(bizType + " directory param in error", ex);  //key not found
            }

            //confirm all the dirs exist
            // if not exist, create and re-confirm OR die
            ConfirmDirExists(dirName: systemConfigDir, createIfMissing: false);
            ConfirmDirExists(dirName: inputRootDir, createIfMissing: false);
            ConfirmDirExists(dirName: workDir, createIfMissing: true);

            //--input file definition json
            //--letter template, letter tags mapping json
            //--other output file def jsons

        }

        private void ConfirmDirExists(string dirName, bool createIfMissing)
        {
            if (File.Exists(dirName))
                return;

            if (createIfMissing)
                File.Create(dirName);

            if (File.Exists(dirName) == false)
            {
                Logger.Write(moduleName, "ConfirmDirExists", 0, dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""), Logger.ERROR);
                throw new Exception(dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""));
            }
        }

        internal bool ProcessNpsLiteApy(string runFor, string operation, string courierCcsv)
        {
            LoadParam("lite");

            //instance based string inputRootDir, string workDir

            Logger.WriteInfo(moduleName, "ProcessNpsLiteApy", 0, $"parameters: {runFor} i/p dir  inputRootDir system dir work dir");
            //Write Log in DB as well - params, process name, start time

            //timer.start

            try
            {
                if (operation == "all" || operation == "read")
                {
                    // process input
                    ProcessNpsLiteApyInput(runFor);
                }

                if (operation == "all" || operation == "write")
                {
                    //process output
                    ProcessNpsLiteApyOutput(runFor, courierCcsv);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "ProcessNpsLiteApy", 0, ex);
                return false;
            }
        }

        private void ProcessNpsLiteApyInput(string runFor)
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
            if (runFor == "all" || runFor == "allover")  //All unprocessed - new Or all - inclluding partially processed
            {
                //scan base directory
                //for each sub directory  -- this can go in parallel, not worth it - mostly 1 date at a time
                string dateAsSubDir = "";
                // collect file names to process - make entry in File Header table with status = "TO DO"
                CollectFilesNpsLiteApyDir(dateAsSubDir, reprocess: true);
            }
            else
            {
                //Process files from  base directory/runFor sub dir only
                CollectFilesNpsLiteApyDir(runFor, reprocess: true);
            }

            //read json file Def for lite apy in systemDir
            //configLiteApyDef = json deserialize 

            //Process files  -- this can go in parallel
            //read File Header table with status = "TO DO"
            //parallel process - pass file Id as param
            //
        }

        internal bool ProcessNpsRegular(string runFor, string operation, string courierCcsv)
        {
            throw new NotImplementedException();
        }

        internal void CollectFilesNpsLiteApyDir(string dateAsDir, bool reprocess)
        {
            // for each file in dir:dateAsDir under 

            //if record not found for the dateAsDir / file name - insert
            //save all details as full input and work path

            //if reprocess == true and record found - update status = TO DO and dateTime of status update
            //if reprocess == false and record found - ignore
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

        private void ProcessNpsLiteApyOutput(string runFor, string courierCcsv)
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
    }
}