using CommonUtil;
using DbOps;
using Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataProcessor
{
    public class ProcessorUtil
    {
        public static Dictionary<string, string> LoadSystemParam(string pgConnection, string pgSchema, string logProgName, string moduleName, int jobId
            , out string systemConfigDir, out string inputRootDir, out string workDir)
        {
            string bizType = ConstantBag.SYSTEM_PARAM;
            return LoadSystemParamByBiz(pgConnection, pgSchema, logProgName, moduleName, bizType, jobId
                , out systemConfigDir, out inputRootDir, out workDir);

        }

            public static Dictionary<string, string> LoadSystemParamByBiz(string pgConnection, string pgSchema, string logProgName, string moduleName, string bizType, int jobId
            , out string systemConfigDir, out string inputRootDir, out string workDir)
        {

            //read details based on date from system param table
            string sysParamStr = DbUtil.GetParamsJsonStr(pgConnection, pgSchema, logProgName, moduleName, bizType, jobId);
            if (sysParamStr == "")
            {
                Logger.Write(logProgName, "LoadParam.1", 0, moduleName + "_" + bizType + " record not in system_param table", Logger.ERROR);
                throw new Exception(moduleName + "_" + bizType + " record not in system_param table");
            }

            Dictionary<string, string> paramsDict;
            try
            {
                paramsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sysParamStr);
                systemConfigDir = paramsDict[ConstantBag.PARAM_SYS_DIR];
                inputRootDir = paramsDict[ConstantBag.PARAM_INP_DIR];
                workDir = paramsDict[ConstantBag.PARAM_WORK_DIR];

                if (systemConfigDir == "" || inputRootDir == "" || workDir == "")
                {
                    Logger.Write(logProgName, "LoadParam.2", 0, moduleName + "_" + bizType + " directory param blank", Logger.ERROR);
                    throw new Exception(moduleName + "_" + bizType + " directory param blank");
                }
                systemConfigDir.TrimEnd('/');
                inputRootDir.TrimEnd('/');
                workDir.TrimEnd('/');
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgName, "LoadParam.2", 0, ex);
                throw new Exception(moduleName + "_" + bizType + " directory param in error", ex);  //key not found
            }

            //confirm all the dirs exist  // if not exist, create and re-confirm OR die
            ConfirmDirExists(logProgName, dirName: systemConfigDir, createIfMissing: false);
            ConfirmDirExists(logProgName, dirName: inputRootDir, createIfMissing: false);
            ConfirmDirExists(logProgName, dirName: workDir, createIfMissing: true);
            return paramsDict;
        }

        public static void ValidateStaticParam(string moduleName, string bizType, string logProgName,  Dictionary<string, string> paramsDict, List<string> staticParamList)
        {
            String erMsg = "";

            foreach (string pName in staticParamList)
            {
                if (!(paramsDict.ContainsKey(pName) && paramsDict[pName] != ""))
                    erMsg += pName + " ";
            }

            if (erMsg != "")
            {
                Logger.Write(logProgName, "ValidateStaticParam", 0, erMsg + " params are missing for " + moduleName + "_" + bizType, Logger.ERROR);
                throw new Exception(erMsg + " params are missing for " + moduleName + "_" + bizType);
            }
        }

        private static void ConfirmDirExists(string logProgName, string dirName, bool createIfMissing)
        {
            if (Directory.Exists(dirName))
                return;

            if (createIfMissing)
                Directory.CreateDirectory(dirName);

            if (Directory.Exists(dirName) == false)
            {
                Logger.Write(logProgName, "ConfirmDirExists", 0, dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""), Logger.ERROR);
                throw new Exception(dirName + " does not exist" + (createIfMissing ? ", cannot create" : ""));
            }
        }

    }
}
