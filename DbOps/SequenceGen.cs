using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DbOps
{
    public class SequenceGen
    {
        private const string logProName = "SequenceGen";

        private static object syncLock = new object();
        private static Dictionary<string, int> courierLocks = new Dictionary<string, int>();  //to make sure no other process or machine is using same courier

        public static string GetNextSequence(bool withLock, string pgConnection, string pgSchema, string seqName, string seqSourceCode
            , string cardType, ref string pattern
            , int fixedLen = -1, bool addIfNeeded = false, bool unlock = false, string freqType = "", string freqValue = "")
        {
            pattern = "";

            bool recFound = true;
            bool dbOk = true;
            int lockKey = 0;
            int counterId = -1;
            string lockOn = seqSourceCode + "_" + freqValue;
            if (freqValue != "")
                addIfNeeded = true;

            string sql1 = "";
            if (withLock)
            {
                if (courierLocks.ContainsKey(lockOn) == false)
                {
                    lock (syncLock)
                    {
                        Random rand = new Random();
                        lockKey = rand.Next(10, 5000);
                        //lock
                        sql1 = $"SELECT {pgSchema}.lock_counter('{seqName}','{seqSourceCode}','{cardType}','{lockKey}','{(addIfNeeded ? "1" : "0")}','{freqType}','{freqValue}')";
                        dbOk = DbUtil.ExecuteScalar(pgConnection, logProName, "", 0, 0, sql1, out counterId, out recFound);

                        if (recFound && counterId > 0)
                        {
                            //add to local static list of locks
                            courierLocks.Add(lockOn, lockKey);
                        }
                    }
                }
                else
                {
                    lockKey = courierLocks[lockOn];
                }

                if (dbOk == false)
                {
                    Logger.Write(logProName, "GetNextSequence", 0, "lck er1 sql " + sql1, Logger.ERROR);
                    throw new Exception("DB ERROR: Could not lock Courier " + lockOn + " make sure no other instance running. Use -op=UNLOCK if sure.");
                }
                if (recFound == false)
                {
                    Logger.Write(logProName, "GetNextSequence", 0, "lck er2 sql " + sql1, Logger.ERROR);
                    throw new Exception("Could not lock Courier " + lockOn + " make sure no other instance running. Use -op=UNLOCK if sure.");
                }
            }

            string sql = $"SELECT {pgSchema}.get_serial_number('{seqName}','{seqSourceCode}', '{cardType}', '{(addIfNeeded ? "1" : "0")}', '{lockKey}', '{freqType}','{freqValue}')";
            lock (syncLock)
            {
                dbOk = DbUtil.ExecuteScalar(pgConnection, logProName, "", 0, 0, sql, out counterId, out recFound);
            }
            if (dbOk == false)
            {
                Logger.Write(logProName, "GetNextSequence", 0, "er1 sql " + sql, Logger.ERROR);
                throw new Exception("DB ERROR: Could not lock Courier " + seqSourceCode + " make sure no other instance running. Use -op=UNLOCK if sure.");
            }
            if (recFound == false || counterId <= 0)
            {
                Logger.Write(logProName, "GetNextSequence", 0, "er2 sql " + sql, Logger.ERROR);
                Logger.Write(logProName, "GetNextSequence", 0, "out: recFound" + recFound + ", counterId" + counterId, Logger.ERROR);
                throw new Exception("Could not get ser number for Courier " + seqSourceCode + " see if there is enough range.");
            }

            string retStr = counterId.ToString();
            if (retStr.Length < fixedLen)
            {
                retStr = retStr.PadLeft(fixedLen, '0');
            }

            pattern = GetPattern(pgConnection, pgSchema, seqName, freqType);

            if (unlock)
            {
                UnlockSeq(pgConnection, pgSchema, seqName, seqSourceCode, lockKey);
            }
            return retStr;
        }

        private static string GetPattern(string pgConnection, string pgSchema, string seqName, string freqType)
        {
            string sql = $"select pat from {pgSchema}.counters where counter_name = '{seqName}' and parent_id = 0";
            if (freqType != "")
            {
                sql += $" and freq_period = '{freqType}'";
            }
            else
            {
                sql += " and coalesce(freq_period, '') = ''";
            }
            try
            {
                DataTable dt = DbUtil.GetDataTab(pgConnection, logProName, "", 0, sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Convert.ToString(dt.Rows[0][0]);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("_" + logProName, "GetPattern", 0, "Error Sql:" + sql, Logger.ERROR);
                Logger.WriteEx("_" + logProName, "GetPattern", 0, ex);
            }

            return "";
        }

        public static void UnlockSeq(string pgConnection, string pgSchema, string seqName, string seqSourceCode, int lockKey)
        {
            DbUtil.Unlock(pgConnection, pgSchema, seqName, seqSourceCode, lockKey);
            courierLocks.Remove(seqSourceCode);
        }

        public static string GetFileDirWithSeq(string dirPath, string subDirPattern, int maxFilesPerSub, int maxDirExpexcted)
        {
            if (Directory.Exists(dirPath) == false)
            {
                Directory.CreateDirectory(dirPath);

            }
            var curSubs = Directory.GetDirectories(dirPath, subDirPattern + "*");

            int maxInUse = 0;
            int tmp;
            string curPartlyUsedDir = "";

            foreach (var sDir in curSubs)
            {
                string justDirNm = new DirectoryInfo(sDir).Name; // such as SIG_001
                string dirNum = justDirNm.Replace(subDirPattern, "");
                if (curPartlyUsedDir == "")
                {
                    curPartlyUsedDir = sDir;
                }

                if (int.TryParse(dirNum, out tmp))
                {
                    if (tmp > maxInUse)  //do not just && above - may be & 
                    {
                        curPartlyUsedDir = sDir;
                        maxInUse = tmp;
                    }
                }
            }
            if (curPartlyUsedDir != "")
            {
                if (Directory.GetFiles(curPartlyUsedDir).Length < maxFilesPerSub)
                {
                    string justDirNm = new DirectoryInfo(curPartlyUsedDir).Name; //currently more files can be added
                    return justDirNm;
                }
            }

            maxInUse++; //next num
            string outSubDir;
            if (maxInUse > maxDirExpexcted)
            {
                outSubDir = subDirPattern + maxInUse;
            }
            else
            {
                outSubDir = maxInUse.ToString();
                tmp = maxDirExpexcted.ToString().Length;

                if (tmp >= 1)
                {
                    outSubDir = outSubDir.PadLeft(tmp, '0');
                }
                outSubDir = subDirPattern + outSubDir;
            }
            return outSubDir;
        }

        public static void UnlockAll(string pgConnection, string pgSchema)
        {
            DbUtil.Unlock(pgConnection, pgSchema);
        }

    }
}
