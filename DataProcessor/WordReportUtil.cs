using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class WordReportUtil
    {
        private string pgConnection;
        private string pgSchema;
        private string moduleName;
        private string bizType;
        private int jobId;
        private RootJsonParamWord wordConfig = null;
        private string outDir;

        private string headerXml = "";
        private string footerXml = "";
        private string mid1Xml = "";
        private string midRepeatXml = "";

        private const string logProgramName = "WordReportUtil";

        public RootJsonParamWord GetWordConfig()
        {
            return wordConfig;
        }

        public WordReportUtil(string connection, string schema, string moduleNm, string bizTypeNm, int jobIdparam, string jsonDef, string outDirNm)
        {
            pgConnection = connection;
            pgSchema = schema;
            moduleName = moduleNm;
            bizType = bizTypeNm;
            jobId = jobIdparam;

            wordConfig = LoadJsonParamFile(jsonDef);
            outDir = outDirNm;
            if (outDir.EndsWith("/") == false)
                outDir = outDir + "/";
        }

        private RootJsonParamWord LoadJsonParamFile(string jsonParamFilePath)
        {
            StreamReader sr = new StreamReader(jsonParamFilePath);
            string fileAsStr = sr.ReadToEnd();

            RootJsonParamWord wordConfig = JsonConvert.DeserializeObject<RootJsonParamWord>(fileAsStr);
            wordConfig.SystemWord.WordWorkDir = wordConfig.SystemWord.WordWorkDir.TrimEnd(new char[] { '\\', '/' });

            SqlHelper.RemoveCommentedPlaceholds(wordConfig.Placeholders);
            return wordConfig;
        }

        public bool CreateFile(string workdirYmd, string fileName, string[] progParams, Dictionary<string, string> paramsDict, DataSet ds, SystemWord systemWord, string waitingAction)
        {
            Stopwatch stopwatch = new Stopwatch();
            //read 4 template files
            ReadBasicTemplates(systemWord, ref headerXml, ref footerXml, ref mid1Xml, ref midRepeatXml);

            int mergeCount = wordConfig.SystemWord.MaxPagesPerFile;

            Logger.WriteInfo(logProgramName, "CreateFile", 0, "All file Create Started, max pages per file:" + mergeCount);

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int curCount = 0;
            int fileCount = 0;

            try
            {
                stopwatch.Start();
                //List<string> usedIds = new List<string>();

                StringBuilder sbMidSect = new StringBuilder();
                bool hasUnPrinted = false;
                int remainCount = ds.Tables[0].Rows.Count;

                Logger.WriteInfo(logProgramName, "CreateMultiPageFiles", 0, "Record count to process:" + remainCount);

                CommandHandler cmdHandler = new CommandHandler();

                List<KeyValuePair<string, string>> tokenMap = new List<KeyValuePair<string, string>>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    curCount++;
                    remainCount--;

                    FillTokenMap(tokenMap, dr, progParams, cmdHandler);
                    AddMidSection(sbMidSect, tokenMap, curCount, mergeCount, remainCount, curCount == 1 ? mid1Xml : midRepeatXml); //, usedIds);
                    hasUnPrinted = true;

                    if (curCount == mergeCount)
                    {
                        WriteDocumentXmlFile(sbMidSect, ref fileCount, ts);
                        curCount = 0;
                        sbMidSect.Clear();
                        //usedIds.Clear();
                        hasUnPrinted = false;
                    }

                    tokenMap.Clear();
                }

                if (hasUnPrinted)
                {
                    WriteDocumentXmlFile(sbMidSect, ref fileCount, ts);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "CreateFile", jobId, ex);
                return false;
            }

            ZipToCreateDocFile(outDir, ts, fileCount);

            /*
                         for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
                        {
                            //save the action for each iRow
                            var dr = ds.Tables[0].Rows[iRow];
                            int detailId = Convert.ToInt32(dr["detail_id"]);
                            bool dbOk= DbUtil.AddAction(pgConnection, pgSchema, logProgramName, moduleName, jobId
                                ,iRow , detailId, waitingAction);
                            if(dbOk == false)
                            {
                                throw new Exception("DB ERROR: RERUN OR manually void/delete actions " + waitingAction + " work dir:" + workdirYmd);
                            }
                        }
             */

            stopwatch.Stop();
            Logger.WriteInfo(logProgramName, "CreateFile", 0, "All files [" + fileCount + "] Created. Time taken in sec:" + stopwatch.Elapsed.TotalSeconds);

            return true;
        }

        private void ZipToCreateDocFile(string outDir, string ts, int fileCount)
        {
            //create/clean up work-subdir
            string workSubDir=  CreateOrCleanWorkSubdir(ts);

            //copy other template files in subdir
            string sourcePath = wordConfig.SystemWord.WordAllFilesDir;
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))  //AllDirectories - if nested
            {
                File.Copy(newPath, newPath.Replace(sourcePath, workSubDir), true);
            }

            for (int iFc = 1; iFc <= fileCount; iFc++)
            {
                //    copy wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + ".xml" to work-subdir
                string src = GetFileNameOfMidXml(iFc, ts);
                File.Copy(src, workSubDir + "\\document.xml");

                //    zip all files as fileName.Replace("{{Serial No}}", i.ToString()).zip
                //    rename zipped file as .docx
                //    move .docx to outdir

                //    delete from work-subdir wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + ".xml"
                File.Delete(workSubDir + "\\document.xml");

                //    delete wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + ".xml"
                File.Delete(src);
            }

            // delete work subdir
            CreateOrCleanWorkSubdir(ts); //delete files in it
            Directory.Delete(workSubDir); //can delete empty sub-dir
        }

        private string CreateOrCleanWorkSubdir(string ts)
        {
            if (Directory.Exists(wordConfig.SystemWord.WordWorkDir + "\\" + ts))
            {
                DirectoryInfo di = new DirectoryInfo(wordConfig.SystemWord.WordWorkDir + "\\" + ts);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(wordConfig.SystemWord.WordWorkDir + "\\" + ts);
            }
            return wordConfig.SystemWord.WordWorkDir + "\\" + ts;
        }

        private string GetFileNameOfMidXml(int fileCount, string ts)
        {
            return wordConfig.SystemWord.WordWorkDir + "\\document_" + ts + "_" + fileCount + ".xml";
        }
        private void WriteDocumentXmlFile(StringBuilder sbMidSect, ref int fileCount, string ts)
        {
            fileCount++;

            string fullOutWorkFile = GetFileNameOfMidXml(fileCount, ts);

            if (File.Exists(fullOutWorkFile))
            {
                MoveOldWorkFile(fileCount, ts, fullOutWorkFile);
            }
            StreamWriter sw = new StreamWriter(fullOutWorkFile, false);

            sbMidSect.Append(footerXml);

            string sFull = headerXml + sbMidSect.ToString();
            sw.Write(sFull);

            sw.Flush();
            Logger.WriteInfo(logProgramName, "WriteDocumentXmlFile", jobId, "created " + fullOutWorkFile);
        }

        private void MoveOldWorkFile(int fileCount, string ts, string fullOutWorkFile)
        {
            int tmp = 1;
            string tmpStr = fullOutWorkFile;
            while (File.Exists(tmpStr))
            {
                tmpStr = wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + "_copy" + tmp + ".xml";
                tmp++;
            }
            File.Move(fullOutWorkFile, tmpStr);
        }

        private void AddMidSection(StringBuilder sbMidSect, List<KeyValuePair<string, string>> tokenMap, int curCount, int mergeCount, int remainCount, string templateXmlBody)
        {
            String sTemplate = new String(templateXmlBody);
            foreach (var tokenVal in tokenMap)
            {
                sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
            }

            //foreach (string idType in templateIds)  -- if we decide to manipulate IDs
            //{
            //    string newIdType = StrUtil.GetNewKeyVal(idType, usedIds);
            //    sTemplate = sTemplate.Replace(idType, newIdType);
            //}
            sbMidSect.Append(sTemplate);

            if (curCount < mergeCount && remainCount > 0)  //except last page - last file's last page
            {
                sbMidSect.Append("<w:br w:type = \"page\" />"); //page break

                //sbMidSect.Append("<w:lastRenderedPageBreak/>");  //page break
            }
        }
        private void FillTokenMap(List<KeyValuePair<string, string>> tokenMap, DataRow dr, string[] progParams, CommandHandler cmdHandler)
        {
            int cellInd = 0;

            for (int i = 0; i < wordConfig.Placeholders.Count; i++)
            {
                ColumnDetail phCol = wordConfig.Placeholders[i];

                string dbVal = "";
                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                {
                    dbVal = cmdHandler.Handle(phCol.DbValue, progParams, dr, out bool isConst); //C Functions are not part of sql select
                }
                else
                {
                    cellInd++;
                    if (dr[cellInd] != DBNull.Value)
                    {
                        dbVal = Convert.ToString(dr[cellInd]);
                    }
                }

                tokenMap.Add(new KeyValuePair<string, string>(phCol.Tag, dbVal));
            }
        }

        private static void ReadBasicTemplates(SystemWord systemWord, ref string headerXml, ref string footerXml, ref string mid1Xml, ref string midRepeatXml)
        {
            using (StreamReader sr = new StreamReader(systemWord.WordHeaderFile))
            {
                headerXml = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(systemWord.WordMiddle1Page))
            {
                mid1Xml = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(systemWord.WordMiddle1Page))
            {
                midRepeatXml = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(systemWord.WordFooterFile))
            {
                footerXml = sr.ReadToEnd();
            }
        }

        private bool GenerateWorkFileName(string fileName, ref string fileName1, ref string fullOutFile, string tmpFileName)
        {
            bool gotIt = false;
            int cnt = 0;
            while (cnt <= 15)
            {
                string serNo = SequenceGen.GetNextSequence(pgConnection, pgSchema, ConstantBag.SEQ_GENERIC
                    , tmpFileName, 2, addIfNeeded: true, unlock: true);  //to do define const for generic
                fileName1 = fileName.Replace("{{Serial No}}", serNo);
                fullOutFile = wordConfig.SystemWord.WordWorkDir + fileName1;

                if (File.Exists(fullOutFile) == false)
                {
                    gotIt = true;
                    break;
                }
                cnt++;
            }
            return gotIt;
        }

        internal bool TemplateFilesExist(string progName)
        {
            bool isOk = true;
            if (File.Exists(wordConfig.SystemWord.WordHeaderFile) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"header file not found {wordConfig.SystemWord.WordHeaderFile}", Logger.ERROR);
            }
            if (File.Exists(wordConfig.SystemWord.WordMiddle1Page) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"Middle Sect Page-1 file not found {wordConfig.SystemWord.WordMiddle1Page}", Logger.ERROR);
            }
            if (File.Exists(wordConfig.SystemWord.WordRepeatPage) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"Middle Sect Repeat Page file not found {wordConfig.SystemWord.WordRepeatPage}", Logger.ERROR);
            }
            if (File.Exists(wordConfig.SystemWord.WordFooterFile) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"footer file not found {wordConfig.SystemWord.WordFooterFile}", Logger.ERROR);
            }

            return isOk;
        }
    }
}
