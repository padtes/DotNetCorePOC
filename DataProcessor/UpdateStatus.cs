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

        public UpdateStatus(string schemaName, string connectionStr, string moduleType, int jobId)
        {
            pgSchema = schemaName;
            pgConnection = connectionStr;
            moduleName = moduleType;
            JobId = jobId;
        }

        public bool Update(string inputFilePathName)
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

            int errCnt = 0;
            List<KeyValuePair<int, string>> updArr = new List<KeyValuePair<int, string>>();
            bool dbOk = ValidateFile(inputFilePathName, maxCount, rejectCodes, updArr, out int lineNo, out errCnt);

            if (dbOk)
            {
                for (int iLineNo = 0; iLineNo < updArr.Count; iLineNo++)
                {
                    dbOk = UpdateDetStatus(iLineNo + 1, updArr[iLineNo].Key, updArr[iLineNo].Value);
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

        private bool ValidateFile(string inputFilePathName, int maxCount, List<string> rejectCodes, List<KeyValuePair<int, string>> updArr, out int lineNo, out int errCnt)
        {
            lineNo = 0;
            errCnt = 0;

            bool dbOk = true;
            string errValCsv;
            char theDelim = ',';
            int BufferSize = 4096;

            string line;
            int detId;

            using (var fileStream = File.OpenRead(inputFilePathName))
            using (var sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    lineNo++;
                    if (lineNo == 1)
                        continue; //skip header

                    string[] cells = line.Split(theDelim);
                    if (IsLineValid(rejectCodes, lineNo, cells, maxCount, out detId, out errValCsv))
                    {
                        updArr.Add(new KeyValuePair<int, string>(detId, errValCsv));
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

        private bool UpdateDetStatus(int lineNo, int detId, string errVal)
        {
            try
            {
                return DbUtil.UpdateDetStatus(pgConnection, pgSchema, logProgramName, moduleName, JobId, lineNo, detId, errVal, actionDone: ConstantBag.DET_LC_STEP_STAT_UPD);
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "UpdateDetStatus", 0, ex);
                return false;
            }
        }

        private bool IsLineValid(List<string> rejectCodes, int lineNo, string[] cells, int maxCount, out int detId, out string errValCsv)
        {
            detId = 0;
            errValCsv = "";
            try
            {
                if (int.TryParse(cells[1], out detId) == false)
                {
                    Logger.Write(logProgramName, "ValidateDetStatus", 0, $"{lineNo} has Non numeric detail Id {cells[1]}", Logger.ERROR);
                    return false;
                }
                if (cells.Length != maxCount)
                {
                    Logger.Write(logProgramName, "ValidateDetStatus", 0, $"{lineNo} cell count {cells.Length} not same as header {maxCount}", Logger.ERROR);
                    return false;
                }

                string err = cells[maxCount - 1].Trim();
                if (string.IsNullOrEmpty(err) == false)
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
                        Logger.Write(logProgramName, "ValidateDetStatus", 0, $"line {lineNo} has wrong reject codes {wrongRejCd}", Logger.ERROR);
                        return false;
                    }
                }
                errValCsv = err.Replace("+", ",");
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "ValidateDetStatus", 0, ex);
                return false;
            }
            return true;
        }

    }
}
