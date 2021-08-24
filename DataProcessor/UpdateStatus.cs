using CommonUtil;
using DbOps;
using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class UpdateStatus
    {
        private const string logProgramName = "UpdateStatus";
        private string pgSchema;
        private string pgConnection;
        private string moduleName;
        private int JobId;
        protected Dictionary<string, string> paramsDict = new Dictionary<string, string>();

        public UpdateStatus(string schemaName, string connectionStr, string moduleType, int jobId)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;
            moduleName = moduleType;
            JobId = jobId;

            string systemConfigDir, inputRootDir, workDir;
            paramsDict = ProcessorUtil.LoadSystemParam(pgConnection, pgSchema, logProgramName, moduleName, JobId
                , out systemConfigDir, out inputRootDir, out workDir);
        }

        public bool Update(bool superUpd, string inputFilePathName)
        {
            if (File.Exists(inputFilePathName) == false)
            {
                Logger.Write(logProgramName, "update", 0, "File not found " + inputFilePathName, Logger.ERROR);
                return false;
            }

            Logger.WriteInfo(logProgramName, "update", 0, "Started " + inputFilePathName);

            //just for ref
            string hdr = SimpleReport.GetHeader();
            string[] hdrCols = hdr.Split(',');
            int maxCount = hdrCols.Length;

            List<string> rejectCodes = new List<string>();
            DbUtil.GetRejectCodes(pgConnection, pgSchema, logProgramName, moduleName, JobId, rejectCodes);
            string printOkCode = paramsDict[ConstantBag.PARAM_PRINTED_OK_CODE];
            if (string.IsNullOrEmpty(printOkCode))
                throw new Exception("Did not find Print Code in ventura.system_param");
            else
                Logger.WriteInfo(logProgramName, "update", 0, "Printed Ok Code " + printOkCode);

            if (rejectCodes.Contains(printOkCode))
                throw new Exception("Print Code in ventura.system_param cannot be in ventura.reject_reasons");

            int errCnt = 0;
            List<UpdStatusStruct> updRecList = new List<UpdStatusStruct>();
            bool dbOk = ValidateFile(superUpd, inputFilePathName, maxCount, rejectCodes, printOkCode, updRecList, out int lineNo, out errCnt);

            if (dbOk)
            {
                for (int iLineNo = 0; iLineNo < updRecList.Count; iLineNo++)
                {
                    dbOk = UpdateDetStatus(iLineNo + 1, updRecList[iLineNo]);
                    if (dbOk == false)
                        errCnt++;
                }
                if (errCnt > 0)
                {
                    dbOk = false;
                    Logger.Write(logProgramName, "update", 0, $"Check logs - there are {errCnt} errors in " + inputFilePathName, Logger.ERROR);
                }
            }
            else
            {
                Logger.Write(logProgramName, "update", 0, $"Check errors. Got {errCnt} errors. No record updated -" + inputFilePathName, Logger.ERROR);
            }

            Logger.WriteInfo(logProgramName, "update", 0, $"{(lineNo - 1)} Finished " + inputFilePathName);

            return dbOk;
        } //update

        private bool ValidateFile(bool superUpd, string inputFilePathName, int maxCount, List<string> rejectCodes
            , string printOkCode, List<UpdStatusStruct> updRecList, out int lineNo, out int errCnt)
        {
            lineNo = 0;
            errCnt = 0;

            bool dbOk = true;
            char theDelim = ',';
            int BufferSize = 4096;

            string line, prnDt, pickDt;
            int detId;

            using (var fileStream = File.OpenRead(inputFilePathName))
            using (var sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    lineNo++;
                    string[] cells = line.Split(theDelim);

                    if (lineNo == 1)
                    {
                        if (cells.Length != maxCount)
                        {
                            Logger.Write(logProgramName, "ValidateFile", 0, $"Header mismatch expecting {maxCount} columns, got {cells.Length}", Logger.ERROR);
                            dbOk = false;
                        }
                        continue; //skip header
                    }

                    if (IsLineValid(rejectCodes, printOkCode, lineNo, cells, maxCount, out detId, out prnDt, out pickDt, out string errValCsv))
                    {
                        bool postOk = IsLinePosting(superUpd, updRecList, lineNo, line, prnDt, detId, errValCsv, ref dbOk);
                        if (postOk == false)
                            errCnt++;
                    }
                    else
                    {
                        errCnt++;
                        dbOk = false;
                    }
                    //dbOk = UpdateDetStatus(lineNo, cells, maxCount) & dbOk;
                }
            }

            return dbOk;
        }

        private bool IsLinePosting(bool superUpd, List<UpdStatusStruct> updRecList, int lineNo, string line, string prnDt, int detId, string errValCsv, ref bool dbOk)
        {
            bool postOk = true;
            bool isFinalStatSent = CheckFinalStatSent(superUpd, lineNo, line, detId);
            if (isFinalStatSent && superUpd == false)
            {
                dbOk = false;
                postOk = false;
            }
            else
            {
                bool isImmRespSent = CheckImmRespSent(superUpd, lineNo, line, detId);
                if (isImmRespSent)
                {
                    updRecList.Add(new UpdStatusStruct
                    {
                        DetId = detId,
                        PrnDtYMD = prnDt,
                        PickDtYMD = prnDt,
                        ErrCsv = errValCsv
                    });
                }
                else
                {
                    dbOk = false;
                    postOk = false;
                }
            }
            return postOk;
        }

        private bool CheckImmRespSent(bool superUpd, int lineNo, string line, int detId)
        {
            bool isImmRespSent = DbUtil.IsActionDone(pgConnection, pgSchema, logProgramName, moduleName, JobId, lineNo
            , detId, ConstantBag.DET_LC_STEP_RESPONSE1);

            if (isImmRespSent == false)
            {
                Logger.Write(logProgramName, "IsLineValid3", 0, $"{lineNo} does not have Immediate resposnce sent. STATUS UPDATE CANNOT BE DONE. Line: {line}", Logger.ERROR);
            }

            return isImmRespSent;
        }

        private bool CheckFinalStatSent(bool superUpd, int lineNo, string line, int detId)
        {
            bool isFinalStatSent = DbUtil.IsActionDone(pgConnection, pgSchema, logProgramName, moduleName, JobId, lineNo
            , detId, ConstantBag.DET_LC_STEP_STAT_REP3);
            if (isFinalStatSent)
            {
                if (superUpd == false)
                {
                    Logger.Write(logProgramName, "IsLineValid2", 0, $"{lineNo} has Final Status Sent. CANNOT UPDATE. Line: {line}", Logger.ERROR);
                }
                else
                {
                    Logger.Write(logProgramName, "IsLineValid2", 0, $"{lineNo} has Final Status Sent. UPDATE OVERRIDE. Line: {line}", Logger.WARNING);
                }
            }

            return isFinalStatSent;
        }

        private bool UpdateDetStatus(int lineNo, UpdStatusStruct statusStruct)
        {
            try
            {
                return DbUtil.UpdateDetStatus(pgConnection, pgSchema, logProgramName, moduleName, JobId,
                    lineNo, statusStruct.DetId, statusStruct.PrnDtYMD, statusStruct.PickDtYMD, statusStruct.ErrCsv
                    , actionDone: ConstantBag.DET_LC_STEP_STAT_UPD2);
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "UpdateDetStatus", 0, ex);
                return false;
            }
        }

        private bool IsLineValid(List<string> rejectCodes, string printOkCode, int lineNo, string[] cells, int maxCount
            , out int detId, out string prnDtStr, out string pickDtStr, out string errValCsv)
        {
            prnDtStr = "";
            pickDtStr = "";
            int pickDtIndx = maxCount - 2;
            int prnDtIndx = maxCount - 3;
            detId = 0;
            errValCsv = "";
            bool isValid = true;

            try
            {
                if (int.TryParse(cells[1], out detId) == false)
                {
                    Logger.Write(logProgramName, "IsLineValid", 0, $"{lineNo} has Non numeric detail Id {cells[1]}", Logger.ERROR);
                    return false;
                }
                if (cells.Length != maxCount)
                {
                    Logger.Write(logProgramName, "IsLineValid", 0, $"{lineNo} cell count {cells.Length} not same as header {maxCount}", Logger.ERROR);
                    return false;
                }

                DateTime dtPrn, dtPick;
                IsDateValid(lineNo, ref isValid, cells[prnDtIndx], "Print dt", out dtPrn, out prnDtStr);
                IsDateValid(lineNo, ref isValid, cells[pickDtIndx], "Pickup", out dtPick, out pickDtStr);

                if (isValid && dtPick < dtPrn)
                {
                    Logger.Write(logProgramName, "IsLineValid", 0, $"{lineNo} Pickup Dt {dtPick} before Print Date {dtPrn} is wrong", Logger.ERROR);
                    isValid = false;
                }

                string err = cells[maxCount - 1].Trim();
                if (string.IsNullOrEmpty(err) )
                {
                    Logger.Write(logProgramName, "IsLineValid", 0, $"line {lineNo} must have reject codes OR " + printOkCode, Logger.ERROR);
                    isValid = false;
                }
                else 
                if (err != printOkCode)
                {
                    string[] errVals = err.Split('+');
                    string wrongRejCd = "";
                    for (int i = 0; i < errVals.Length; i++)
                    {
                        if (rejectCodes.Contains(errVals[i]) == false)
                            wrongRejCd += " " + errVals[i];
                    }
                    if (wrongRejCd != "")
                    {
                        Logger.Write(logProgramName, "IsLineValid", 0, $"line {lineNo} has wrong reject codes {wrongRejCd}", Logger.ERROR);
                        isValid = false;
                    }
                }
                errValCsv = err.Replace("+", ",");
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "IsLineValid", 0, ex);
                return false;
            }
            return isValid;
        }

        private static void IsDateValid(int lineNo, ref bool isValid, string dtStr, string dtNm, out DateTime dt, out string dtStrYMD)
        {
            dt = DateTime.MinValue;
            dtStrYMD = "";
            if (string.IsNullOrEmpty(dtStr) == false)
            {
                if (dtStr.StartsWith("'"))
                    dtStr = dtStr.Substring(1);

                if (DateTime.TryParse(dtStr, out dt) == false)
                {
                    Logger.Write(logProgramName, "IsDateValid", 0, $"line {lineNo} has wrong {dtNm} date {dtStr}", Logger.ERROR);
                    isValid = false;
                }
                else
                {
                    dtStrYMD = dt.ToString("yyyy/MM/dd HH:mm:ss");
                }
            }
        }
    }

    internal class UpdStatusStruct
    {
        internal int DetId { get; set; }
        internal string PrnDtYMD { get; set; }
        internal string PickDtYMD { get; set; }
        internal string ErrCsv { get; set; }
    }
}
