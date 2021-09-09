using CommonUtil;
using DbOps;
using DbOps.Structs;
using Ionic.Zip;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Security;
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
        private string midXml = "";

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

        public bool CreateFile(string workdirYmd, string courierCd, string fileName, string[] progParams, Dictionary<string, string> paramsDict, DataSet ds, SystemWord systemWord)
        {
            Stopwatch stopwatch = new Stopwatch();
            //read 4 template files
            ReadBasicTemplates(systemWord, ref headerXml, ref footerXml, ref midXml);

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

                    FillTokenMap(tokenMap, dr, progParams, paramsDict, cmdHandler);
                    //if header or footer has tags, it will need to be addressed here when curCount == 1
                    AddMidSection(sbMidSect, tokenMap, curCount, mergeCount, remainCount, midXml); //, usedIds);
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

            ZipToCreateDocFile(outDir, fileName, ts, fileCount, courierCd, workdirYmd);

            for (int iRow = 0; iRow < ds.Tables[0].Rows.Count; iRow++)
            {
                //save the action for each iRow
                var dr = ds.Tables[0].Rows[iRow];
                int detailId = Convert.ToInt32(dr["detail_id"]);
                try
                {
                    bool dbOk = DbUtil.AddAction(pgConnection, pgSchema, logProgramName, moduleName, jobId
                    , iRow, detailId, actionDone: ConstantBag.DET_LC_STEP_WORD_LTR4);
                }
                catch
                {
                    Logger.WriteInfo(logProgramName, "CreateFile", 0, $"error det id {detailId}. Verify action added " + ConstantBag.DET_LC_STEP_WORD_LTR4);
                }
            }

            stopwatch.Stop();
            Logger.WriteInfo(logProgramName, "CreateFile", 0, "All files [" + fileCount + "] Created. Time taken in sec:" + stopwatch.Elapsed.TotalSeconds);

            return true;
        }

        private void ZipToCreateDocFile(string outDir, string fNamePattern, string ts, int fileCount, string courierCd, string dateAsDir)
        {
            //create/clean up work-subdir
            string workSubDir = CreateOrCleanWorkSubdir(ts);
            //copy other template files in subdir
            string sourcePath = wordConfig.SystemWord.WordAllFilesDir;

            FIleIOExt.CopyDirDeep(sourcePath, workSubDir);

            for (int iFc = 1; iFc <= fileCount; iFc++)
            {
                //    copy wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + ".xml" to work-subdir
                string src = GetFileNameOfMidXml(iFc, ts);
                File.Copy(src, Path.Combine(new string[] { workSubDir, "word", "document.xml" }), overwrite: true);

                string pureFn = fNamePattern.Replace(ConstantBag.FILE_NAME_TAG_SER_NO, iFc.ToString())
                    .Replace(ConstantBag.FILE_NAME_TAG_COUR_CD, courierCd)
                    .Replace(ConstantBag.FILE_NAME_TAG_YYMMDD, dateAsDir);  //to do : find ser # and make part of FileName

                if (pureFn.ToLower().EndsWith(".docx") == false)
                {
                    pureFn += ".docx";
                }
                string[] tmpFiles = Directory.GetFiles(workSubDir, "*.*");
                string[] tmpDirs = Directory.GetDirectories(workSubDir);
                using (ZipFile zip = new ZipFile())
                {
                    //    zip all files as word...zip - docx
                    foreach (string dirNm in tmpDirs)
                    {
                        DirectoryInfo din = new DirectoryInfo(dirNm);
                        zip.AddDirectory(dirNm, din.Name);
                    }
                    foreach (string fileNm in tmpFiles)
                    {
                        zip.AddFile(fileNm, "");
                    }

                    zip.Save(workSubDir + "\\" + pureFn);
                }

                //    move .docx to outdir
                string destDir = Path.Combine(new string[] { outDir, courierCd + "_" + dateAsDir, "Letters" }); //to do parameterize for Letters
                //  outDir + "\\" + courierCd + "_" + dateAsDir + "\\Letters";   
                if (Directory.Exists(destDir) == false)
                {
                    Directory.CreateDirectory(destDir);
                }
                int tmp = 1;
                string tmpStr = destDir + "\\" + pureFn;
                if (File.Exists(tmpStr))
                {
                    while (File.Exists(tmpStr))
                    {
                        tmpStr = destDir + "\\copy" + tmp + "_" + pureFn;
                        tmp++;
                    }
                    File.Move(destDir + "\\" + pureFn, tmpStr);
                }

                File.Move(workSubDir + "\\" + pureFn, destDir + "\\" + pureFn);

                //    delete wordConfig.SystemWord.WordWorkDir + "/document_" + ts + "_" + fileCount + ".xml"
                File.Delete(src);
            }

            // delete work subdir
            CreateOrCleanWorkSubdir(ts); //delete files in it
            Directory.Delete(workSubDir, true); //can delete empty sub-dir
        }

        private string CreateOrCleanWorkSubdir(string ts)
        {
            string t1 = Path.Combine(wordConfig.SystemWord.WordWorkDir, ts);

            if (Directory.Exists(t1))
            {
                DirectoryInfo di = new DirectoryInfo(t1);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(t1);
            }
            return t1;
        }

        private string GetFileNameOfMidXml(int fileCount, string ts)
        {
            if (Directory.Exists(wordConfig.SystemWord.WordWorkDir) == false)
            {
                Directory.CreateDirectory(wordConfig.SystemWord.WordWorkDir);
            }
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

            sbMidSect.Append(footerXml.Trim(new char[] { '\n', '\r' }));

            string sFull = headerXml.Trim(new char[] { '\n', '\r' }) + sbMidSect.ToString();

            sw.Write(sFull.Replace("\n", "").Replace("\r", ""));

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
            string sTemplate = new String(templateXmlBody);
            foreach (var tokenVal in tokenMap)
            {
                //sTemplate = sTemplate.Replace(tokenVal.Key, tokenVal.Value);
                sTemplate = sTemplate.ReplaceAlt(tokenVal.Key, tokenVal.Value, StringComparison.OrdinalIgnoreCase);
            }

            //foreach (string idType in templateIds)  -- if we decide to manipulate IDs
            //{
            //    string newIdType = StrUtil.GetNewKeyVal(idType, usedIds);
            //    sTemplate = sTemplate.Replace(idType, newIdType);
            //}
            if (curCount > 1)
            {
                sTemplate = sTemplate.Replace(ConstantBag.TAG_WORD_NEW_PAGE, "<w:pageBreakBefore/>"); //page break after the 1st
            }
            else
                sTemplate = sTemplate.Replace(ConstantBag.TAG_WORD_NEW_PAGE, string.Empty); //No page break on the 1st

            sbMidSect.Append(sTemplate);

            //if (curCount < mergeCount && remainCount > 0)  //except last page - last file's last page
            //{
            //    //sbMidSect.Append("<w:br w:type = \"page\" />"); //page break
            //    sbMidSect.Append("<w:lastRenderedPageBreak/>");  //page break
            //}
        }
        private void FillTokenMap(List<KeyValuePair<string, string>> tokenMap, DataRow dr, string[] progParams, Dictionary<string, string> paramsDict, CommandHandler cmdHandler)
        {
            int cellInd = 0;

            for (int i = 0; i < wordConfig.Placeholders.Count; i++)
            {
                ColumnDetail phCol = wordConfig.Placeholders[i];

                string dbVal = "";
                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                {
                    dbVal = cmdHandler.Handle(phCol.DbValue, progParams, dr, paramsDict, out bool isConst); //C Functions are not part of sql select
                }
                else
                {
                    if (dr[cellInd] != DBNull.Value)
                    {
                        dbVal = Convert.ToString(dr[cellInd]);
                    }
                    cellInd++;
                }
                if (dbVal.Contains("<w:br/>"))
                {
                    string tmpDbVal = dbVal.Replace("<w:br/>", "!!:br/$");
                    tmpDbVal = SecurityElement.Escape(tmpDbVal);
                    dbVal = tmpDbVal.Replace("!!:br/$", "<w:br/>");
                }
                else
                    dbVal = SecurityElement.Escape(dbVal);

                tokenMap.Add(new KeyValuePair<string, string>(phCol.Tag, dbVal));
            }
        }

        private static void ReadBasicTemplates(SystemWord systemWord, ref string headerXml, ref string footerXml, ref string midXml)
        {
            using (StreamReader sr = new StreamReader(systemWord.WordHeaderFile))
            {
                headerXml = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(systemWord.WordMiddlePage))
            {
                midXml = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(systemWord.WordFooterFile))
            {
                footerXml = sr.ReadToEnd();
            }
        }

        //private bool GenerateWorkFileName(string fileName, ref string fileName1, ref string fullOutFile, string tmpFileName)
        //{
        //    bool gotIt = false;
        //    int cnt = 0;
        //    while (cnt <= 15)
        //    {
        //        string serNo = SequenceGen.GetNextSequence(pgConnection, pgSchema, ConstantBag.SEQ_GENERIC
        //            , tmpFileName, 2, addIfNeeded: true, unlock: true);  //to do define const for generic
        //        fileName1 = fileName.Replace(ConstantBag.FILE_NAME_TAG_SER_NO, serNo);
        //        fullOutFile = wordConfig.SystemWord.WordWorkDir + fileName1;

        //        if (File.Exists(fullOutFile) == false)
        //        {
        //            gotIt = true;
        //            break;
        //        }
        //        cnt++;
        //    }
        //    return gotIt;
        //}

        internal bool TemplateFilesExist(string progName)
        {
            bool isOk = true;
            if (File.Exists(wordConfig.SystemWord.WordHeaderFile) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"header file not found {wordConfig.SystemWord.WordHeaderFile}", Logger.ERROR);
            }
            if (File.Exists(wordConfig.SystemWord.WordMiddlePage) == false)
            {
                isOk = false;
                Logger.Write(progName, "TemplateFilesExist", 0, $"Middle Sect Page file not found {wordConfig.SystemWord.WordMiddlePage}", Logger.ERROR);
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
