using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DbOps
{
    public class SequenceGen
    {
        private const string logProName = "SequenceGen";

        private static object syncLock = new object();
        private static Dictionary<string, int> courierLocks = new Dictionary<string, int>();  //to make sure no other process or machine is using same courier

        public static string GetNextSequence(string pgConnection, string pgSchema, string seqName, string seqSourceCode
            , int fixedLen = -1, bool addIfNeeded = false, bool unlock = false)
        {
            bool recFound = true;
            bool dbOk = false;
            int lockKey = -9;
            int counterId = -1;

            if (courierLocks.ContainsKey(seqSourceCode) == false)
            {
                lock (syncLock)
                {
                    Random rand = new Random();
                    lockKey = rand.Next(10, 5000);
                    //lock
                    string sql1 = $"SELECT {pgSchema}.lock_counter('{seqName}','{seqSourceCode}','{lockKey}','{(addIfNeeded?"1":"0")}')";
                    dbOk = DbUtil.ExecuteScalar(pgConnection, logProName, "", 0, 0, sql1, out counterId, out recFound);

                    if (recFound && counterId > 0)
                    {
                        courierLocks.Add(seqSourceCode, lockKey);
                    }
                }
            }
            else
            {
                lockKey = courierLocks[seqSourceCode];
            }

            if (dbOk == false)
            {
                throw new Exception("DB ERROR: Could not lock Courier " + seqSourceCode + " make sure no other instance running. Use -op=UNLOCK if sure.");
            }
            if (recFound == false)
            {
                throw new Exception("Could not lock Courier " + seqSourceCode + " make sure no other instance running. Use -op=UNLOCK if sure.");
            }

            //add to local static list of locks
            string sql = $"SELECT {pgSchema}.get_serial_number('{seqName}','{seqSourceCode}', '0', '{lockKey}')";
            lock (syncLock)
            {
                dbOk = DbUtil.ExecuteScalar(pgConnection, logProName, "", 0, 0, sql, out counterId, out recFound);
            }
            if (dbOk == false)
            {
                throw new Exception("DB ERROR: Could not lock Courier " + seqSourceCode + " make sure no other instance running. Use -op=UNLOCK if sure.");
            }
            if (recFound == false || counterId <= 0)
            {
                throw new Exception("Could not get ser number for Courier " + seqSourceCode + " see if there is enough range.");
            }

            string retStr = counterId.ToString();
            if (retStr.Length < fixedLen)
            {
                retStr = retStr.PadLeft(fixedLen, '0');
            }

            if(unlock)
            {
                DbUtil.Unlock(pgConnection, pgSchema, seqName, seqSourceCode, lockKey);
                courierLocks.Remove(seqSourceCode);
            }
            return retStr;
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
