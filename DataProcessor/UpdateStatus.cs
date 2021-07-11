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

            int lineNo = 0;
            string line;
            int BufferSize = 4096;
            char theDelim = ',';
            bool dbOk = true;

            //just for ref
            string hdr = SimpleReport.GetHeader();
            string[] hdrCols = hdr.Split(',');
            int maxCount = hdrCols.Length;

            using (var fileStream = File.OpenRead(inputFilePathName))
            using (var sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    lineNo++;
                    if (lineNo == 1)
                        continue; //skip header

                    string[] cells = line.Split(theDelim);
                    dbOk = UpdateDetStatus(lineNo, cells, maxCount) & dbOk;
                }
            }

            Logger.WriteInfo(logProgramName, "update", 0, $"{(lineNo - 1)} Finished " + inputFilePathName);
            if (dbOk == false)
                Logger.Write(logProgramName, "update", 0, "Check logs - there are errors in " + inputFilePathName, Logger.ERROR);

            return dbOk;
        } //update

        private bool UpdateDetStatus(int lineNo, string[] cells, int maxCount)
        {
            try
            {
                int detId = 0;
                if (int.TryParse(cells[1], out detId) == false)
                {
                    Logger.Write(logProgramName, "UpdateDetStatus", 0, $"{lineNo} has Non numeric detail Id {cells[1]}", Logger.ERROR);
                    return false;
                }
                if (cells.Length != maxCount)
                {
                    Logger.Write(logProgramName, "UpdateDetStatus", 0, $"{lineNo} cell count {cells.Length} not same as header {maxCount}", Logger.ERROR);
                    return false;
                }
                string err = cells[maxCount - 1].Trim();
                err = err.Replace("+", ",");

                return DbUtil.UpdateDetStatus(pgConnection, pgSchema, logProgramName, moduleName, JobId, lineNo, detId, err);
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "UpdateDetStatus", 0, ex);
                return false;
            }
        }
    }
}
