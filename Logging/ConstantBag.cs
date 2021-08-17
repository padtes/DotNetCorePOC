namespace CommonUtil
{
    public class ConstantBag
    {
        //default user for Added By / Updated By
        public const string BATCH_USER = "batch";
        public const string SYSTEM_PARAM = "system";
        public const string SEQ_GENERIC = "generic";

        //public const string LITE_OUT_PARAM = "lite_out";

        //Modules
        public const string MODULE_LITE = "lite";
        public const string MODULE_REG = "reg";

        //db actions
        public const string IGNORED = "IGNORE";
        public const string INSERTED = "INS";
        public const string UPDATED = "UPD";

        //Lite bussiness
        public const string LITE_IN = "lite_inp";
        public const string LITE_OUT_RESPONSE = "lite_resp";
        public const string LITE_OUT_STATUS = "lite_stat";
        public const string LITE_OUT_WORD_APY = "lite_word_apy";
        public const string LITE_OUT_WORD_NPS = "lite_word_nps";
        public const string LITE_OUT_CARD_APY = "lite_card_apy";
        public const string LITE_OUT_CARD_NPS = "lite_card_nps";
        public const string LITE_OUT_PTC_APY = "lite_ptc_apy";
        public const string LITE_OUT_PTC_NPS = "lite_ptc_nps";
        public const string LITE_OUT_AWB_APY = "lite_awb_apy";
        public const string LITE_OUT_AWB_NPS = "lite_awb_nps";

        //card types 
        public const string APY_FLAG_DB_COL_NAME = "apy_flag";
        public const string CARD_NA = "";
        public const string CARD_APY = "apy";
        public const string CARD_LITE = "lite";
        // public const string CARD_REG = "REG";  // these can be decided later
       
        //file directions
        public const string DIRECTION_IN = "i";
        public const string DIRECTION_OUT = "o";
        public const string DIRECTION_INTERNAL = "p";

        //file life cycle steps
        public const string FILE_LC_STEP_TODO = "TO-DO";
        public const string FILE_LC_WIP = "WIP";  //work in progress
        public const string FILE_LC_STEP_TO_DB = "DB_DONE";
        public const string FILE_LC_STEP_ERR1 = "ERROR_1";

        //file detail record life cycle 
        public const string DET_LC_STEP_RESPONSE1 = "1IMM_RESP";  //immediate response sent
        public const string DET_LC_STEP_STAT_UPD2 = "2STATUS_UPD"; //status updated
        public const string DET_LC_STEP_STAT_REP3 = "3STATUS_REP"; //status report sent
        public const string DET_LC_STEP_WORD_LTR4 = "4WORD_REP"; //Word Letters printed
        public const string DET_LC_STEP_CARD_OUT5 = "5CARD_OUT"; //Card done
        public const string DET_LC_STEP_PTC_REP6 = "6PTC_REP"; //Printer to Courier done
        public const string DET_LC_STEP_AWB_REP7 = "7PTC_REP"; //AWB report

        //public const string FILE_NAME_TAG_UNIQUE_COL = "{{unique_column}}";
        //public const string FILE_NAME_TAG_REC_ID = "{{record_id}}";
        //public const string FILE_NAME_TAG_COUR_SEQ = "{{courier_seq}}";
        public const string FILE_NAME_TAG_COUR_CD = "{{courier_cd}}";
        public const string FILE_NAME_TAG_SER_NO = "{{serial_no}}";
        public const string FILE_NAME_TAG_YYMMDD = "{{yyyymmdd}}";
        public const string TAG_START = "{{";
        public const string TAG_END = "}}";
        public const string TAG_REG_PAT = @"\{{(.*?)\}}";
        public const string TAG_WORD_NEW_PAGE = "{{X_SYS_NEWDOC}}";

        //All Module parameter
        public const string PARAM_SYS_DIR = "systemdir";
        public const string PARAM_INP_DIR = "inputdir";
        public const string PARAM_WORK_DIR = "workdir";

        //Lite Module parameter
        public const string PARAM_OUTPUT_PARENT_DIR = "output_par";
        public const string PARAM_OUTPUT_LITE_DIR = "output_lite";
        public const string PARAM_OUTPUT_APY_DIR = "output_apy";
        public const string PARAM_IMAGE_LIMIT = "photo_max_per_dir";
        public const string PARAM_SUBDIR_APROX_LIMIT = "expect_max_subdir";
        public const string PARAM_PRINTER_CODE2 = "printer_code2";
        public const string PARAM_PRINTER_CODE3 = "printer_code3";

        //file definition descriptors
        public const string FD_SYSTEM_PARAM = "system";
        public const string FD_FILE_TYPE = "file_type";
        public const string FD_DELIMT = "delimt";
        public const string FD_INDEX_OF_ROW_TYPE = "index_of_row_type";
        public const string FD_FILE_HEADER_ROW_TYPE = "file_header_row_type";
        public const string FD_DATA_ROW_TYPE = "data_row_type";
        public const string FD_DATA_TABLE_NAME = "data_table_name";
        public const string FD_DATA_TABLE_JSON_COL = "data_table_json_col";
        public const string FD_UNIQUE_COLUMN = "unique_column";
        public const string FD_COURIER_COL = "courier_col";
        // 3 flags - card print | letter | PTC entry
        // introduce batch concept

    }

}