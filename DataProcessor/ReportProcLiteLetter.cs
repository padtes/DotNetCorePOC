using CommonUtil;
using DbOps;
using DbOps.Structs;
using Logging;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataProcessor
{
    public class ReportProcLiteLetter : ReportProcessorLite
    {
        public ReportProcLiteLetter(string connectionStr, string schemaName, string module, string opName, string fileType)
            : base(connectionStr, schemaName, module, opName, fileType)
        {
        }

        public override string GetProgName()
        {
            return "ReportProcLiteLetter";
        }

        protected override void SetupActions(string bizTypeToWrite, out string waitingAction, out string doneAction)
        {
            if (bizTypeToWrite == ConstantBag.LITE_OUT_WORD_APY || bizTypeToWrite == ConstantBag.LITE_OUT_WORD_NPS)
            {
                waitingAction = ConstantBag.DET_LC_STEP_STAT_REP3;
                doneAction = "";
            }
            else
                base.SetupActions(bizTypeToWrite, out waitingAction, out doneAction);
        }

        public override void WriteOutput(string runFor, string courierCsv)
        {
            if (fileType == "letter" || fileType == "let_reprint") //letters //to do define const
            {
                WriteWordFiles(runFor, courierCsv, fileType == "let_reprint");
            }
            else
            {
                base.WriteOutput(runFor, courierCsv);
            }
        }
        private void WriteWordFiles(string runFor, string courierCsv, bool reprint)
        {
            string bizTypeToRead = ConstantBag.LITE_IN;
            string bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_LITE_DIR];
            ProcessNpsApyWord(bizTypeToRead, runFor, bizDir, false, courierCsv, reprint);

            bizDir = paramsDict[ConstantBag.PARAM_OUTPUT_APY_DIR];
            ProcessNpsApyWord(bizTypeToRead, runFor, bizDir, true, courierCsv, reprint);
        }
        private void ProcessNpsApyWord(string bizTypeToRead, string workdirYmd, string bizDir, bool isApy, string courierCsv, bool reprint)
        {
            string bizType = isApy ? ConstantBag.LITE_OUT_WORD_APY : ConstantBag.LITE_OUT_WORD_NPS;

            FileTypeMaster fTypeMaster = GetFTypeMaster(bizType);
            if (fTypeMaster == null)
                return;

            //collect what all couriers to process
            List<string> courierList = new List<string>();
            string waitingAction = ConstantBag.DET_LC_STEP_WORD_LTR4;
            string doneAction = ConstantBag.DET_LC_STEP_RESPONSE1;
            if (reprint)
            {
                doneAction = ConstantBag.DET_LC_STEP_WORD_LTR4;
                waitingAction = "";
            }

            DbUtil.GetCouriers(GetConnection(), GetSchema(), GetProgName(), moduleName, bizTypeToRead, JobId
                , workdirYmd, waitingAction, doneAction, courierList, isApy, courierCsv, out string sql);

            Logger.Write(GetProgName(), "ProcessNpsApyWord", 0, "sql:" + sql, Logger.INFO);
            if (courierList.Count < 1)
            {
                Logger.Write(GetProgName(), "ProcessNpsApyWord", 0, $"No records found for letters {workdirYmd} cour: {courierCsv} {(isApy ? "Apy" : "NPS")}", Logger.WARNING);
                return;
            }

            Logger.WriteInfo(GetProgName(), "ProcessNpsApyWord", 0, $"{string.Join(',', courierList.ToArray())} found for letters {workdirYmd} cour: {courierCsv} {(isApy ? "Apy" : "NPS")}");

            string outputDir = paramsDict[ConstantBag.PARAM_WORK_DIR]
            + "\\" + workdirYmd// "yyyymmdd" 
            + "\\" + paramsDict[ConstantBag.PARAM_OUTPUT_PARENT_DIR]
            + "\\" + bizDir // module + bizType based dir = ("output_apy or output_lite") 
            ;

            string jsonCsvDef = paramsDict[ConstantBag.PARAM_SYS_DIR] + "\\" + fTypeMaster.fileDefJsonFName;

            WordReportUtil wordUtil = new WordReportUtil(pgConnection, pgSchema, moduleName, bizType, JobId, jsonCsvDef, outputDir);
            if (wordUtil.TemplateFilesExist(GetProgName()) == false)
            {
                return;
            }

            foreach (string courierCd in courierList)
            {
                ProcessNpsApyWordCourier(wordUtil, bizTypeToRead, bizType, fTypeMaster, workdirYmd, bizDir, isApy, courierCd);
            }
        }
        private void ProcessNpsApyWordCourier(WordReportUtil wordUtil, string bizTypeToRead, string bizTypeToWrite, FileTypeMaster fTypeMaster, string workdirYmd, string bizDir, bool isApy, string courierCd)
        {
            RootJsonParamWord wordConfig = wordUtil.GetWordConfig();

            string waitingAction, doneAction;
            SetupActions(bizTypeToWrite, out waitingAction, out doneAction);

            string[] args = { }; //DateTime.Now.ToString("dd-MMM-yyyy")  ?? program params if any

            StringBuilder colSelectionSb = new StringBuilder();
            SqlHelper.GetSelectColumns1(wordConfig.Placeholders, wordConfig.SystemWord, args, paramsDict, colSelectionSb);

            ////get records for the courier
            DataSet ds = DbUtil.GetLetterCourier(GetConnection(), GetSchema(), GetProgName(), moduleName, bizTypeToRead, JobId
                , workdirYmd, waitingAction, doneAction
                , courierCd, isApy, colSelectionSb.ToString(), wordConfig.SystemWord.DataOrderby, out string sql);

            Logger.Write(GetProgName(), "ProcessNpsApyWordCourier", 0, $"{sql}", Logger.INFO);

            wordUtil.CreateFile(workdirYmd, courierCd, fTypeMaster.fnamePattern, args, paramsDict, ds, wordConfig.SystemWord);
        }

    }
}
