using System.Collections.Generic;

namespace CommonUtil
{
    public class ConstantBag
    {
        //default user for Added By / Updated By
        public const string BATCH_USER = "batch";
        public const string SYSTEM_PARAM = "system";
        public const string SEQ_GENERIC = "generic";
        public const string JROOT = "root";
        public const string ALL = "all";
        //public const string LITE_OUT_PARAM = "lite_out";

        //Modules
        public const string MODULE_LITE = "lite";
        public const string MODULE_REG = "reg";
        public const string MODULE_PAN = "pan";

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

        //pan business
        public const string PAN_IN = "pan_inp";
        public const string PAN_INDIV = "pan_ind";
        public const string PAN_CORP = "pan_corp";
        public const string PAN_EKYC = "pan_ekyc";

        /* PAN
            a b c 
            . . I = Individual
            . . C = Corp

            . R . = Original
            . C . = Change Address
            . L . = Change Name

            P . . = Primary First-time print
            R . . = Reprint
        + eKYC as
            EkycPRI - new
                EkycPCI - change 
                EkycPLI - change 
            --- reprint --
                EkycRRI - new
                EkycRCI - change 
                EkycRLI - change
         */
        //public const string PAN_MAIN = "_MAIN";
        //public const string PAN_PR_INDIV = "PRI"; //First time Original Individual
        //public const string PAN_PC_INDIV = "PCI"; //First time with Address Change Individual
        //public const string PAN_PL_INDIV = "PLI"; //First time with Name Change Individual
        //public const string PAN_RR_INDIV = "RRI"; //Reprint Original Individual
        //public const string PAN_RC_INDIV = "RCI"; //Reprint with Address Change Individual
        //public const string PAN_RL_INDIV = "RLI"; //Reprint with Name Change Individual

        //public static readonly List<string> PAN_VALID_SUB_INDIV = new List<string>() { "PRI", "PCI", "PLI", "RRI", "RCI", "RLI" };

        //public const string PAN_PR_CORP = "PRC"; //First time Original Corporate
        //public const string PAN_PC_CORP = "PCC"; //First time with Address Change Corporate
        //public const string PAN_PL_CORP = "PLC"; //First time with Name Change Corporate
        //public const string PAN_RR_CORP = "RRC"; //Reprint Original Corporate
        //public const string PAN_RC_CORP = "RCC"; //Reprint with Address Change Corporate
        //public const string PAN_RL_CORP = "RLC"; //Reprint with Name Change Corporate

        //public static readonly List<string> PAN_VALID_SUB_CORP = new List<string>() { "PRC", "PCC", "PLC", "RRC", "RCC", "RLC" };

        //public const string PAN_PR_EKYC = "PRIEKYC"; //First time Original Corporate
        //public const string PAN_PC_EKYC = "PCIEKYC"; //First time with Address Change Corporate
        //public const string PAN_PL_EKYC = "PLIEKYC"; //First time with Name Change Corporate
        //public const string PAN_RR_EKYC = "RRIEKYC"; //Reprint Original Corporate
        //public const string PAN_RC_EKYC = "RCIEKYC"; //Reprint with Address Change Corporate
        //public const string PAN_RL_EKYC = "RLIEKYC"; //Reprint with Name Change Corporate

        //public static readonly List<string> PAN_VALID_SUB_EKYC = new List<string>() { "PRIEKYC", "PCIEKYC", "PLIEKYC", "RRIEKYC", "RCIEKYC", "RLIEKYC" };

        //public const string PAN_OUT_RESPONSE_INDV = "pan_resp_ind";
        //public const string PAN_OUT_RESPONSE_CORP = "pan_resp_corp";
        //public const string PAN_OUT_RESPONSE_EKYC = "pan_resp_ekyc";
        public const string PAN_OUT_CARD_INDV = "pan_card_ind";
        public const string PAN_OUT_CARD_CORP = "pan_card_corp";
        public const string PAN_OUT_CARD_EKYC = "pan_card_ekyc";
        public const string PARAM_OUTPUT_DIR_PAN_INDV = "output_pan_ind";
        public const string PARAM_OUTPUT_DIR_PAN_CORP = "output_pan_corp";
        public const string PARAM_OUTPUT_DIR_PAN_EKYC = "output_pan_ekyc";

        public const string PAN_STEP_CARD_OUT = "PAN_CARD_OUT"; //Card done

        //card types 
        public const string APY_FLAG_DB_COL_NAME = "apy_flag";
        public const string CARD_NA = "";
        public const string CARD_APY = "apy";
        public const string CARD_LITE = "lite";
        public const string CARD_PAN = "pan";

        // public const string CARD_REG = "REG";  // these can be decided later
       
        //file directions
        public const string DIRECTION_IN = "i";
        public const string DIRECTION_OUT = "o";
        public const string DIRECTION_INTERNAL = "p";

        //file life cycle steps
        public const string FILE_LC_STEP_TODO = "TO-DO";
        public const string FILE_LC_STEP_TODO_PAN = "TO-DO-P";
        public const string FILE_LC_WIP = "WIP";  //work in progress
        public const string FILE_LC_WIP_PAN = "WIP-P";  //work in progress
        public const string FILE_LC_STEP_TO_DB = "DB_DONE";
        public const string FILE_LC_STEP_ERR1 = "ERROR_1";
        public const string FILE_LC_STEP_WARN_DUP = "WARN_DUP";

        //file detail record life cycle 
        public const string DET_LC_STEP_RESPONSE1 = "1IMM_RESP";  //immediate response sent
        public const string DET_LC_STEP_STAT_UPD2 = "2STATUS_UPD"; //status updated
        public const string DET_LC_STEP_STAT_REP3 = "3STATUS_REP"; //status report sent
        public const string DET_LC_STEP_WORD_LTR4 = "4WORD_REP"; //Word Letters printed
        public const string DET_LC_STEP_CARD_OUT5 = "5CARD_OUT"; //Card done
        public const string DET_LC_STEP_PTC_REP6 = "6PTC_REP"; //Printer to Courier done
        public const string DET_LC_STEP_AWB_REP7 = "7AWB_REP"; //AWB report

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
        public const string PARAM_COURIER_KVCSV = "courier_awb_kvcsv";
        public const string PARAM_PRINTED_OK_CODE = "printed_ok_code";

        public const string PARAM_PAN_OUTPUT_PARENT_DIR = "pan_output_par";
        public const string PARAM_PAN_OUTPUT_DIR = "pan_output";
        public const string PARAM_PAN_FILE_GROUP = "pan_filegroup_csv"; //what all grouping


        //input file definition descriptors
        public const string FD_SYSTEM_PARAM = "system";
        public const string FD_FILE_TYPE = "file_type";
        public const string FD_DELIMT = "delimt";
        public const string FD_INDEX_OF_ROW_TYPE = "index_of_row_type";
        public const string FD_FILE_HEADER_ROW_TYPE = "file_header_row_type";
        public const string FD_DATA_ROW_TYPE = "data_row_type";
        public const string FD_DATA_TABLE_NAME = "data_table_name";
        public const string FD_DATA_TABLE_JSON_COL = "data_table_json_col";
        public const string FD_UNIQUE_COLUMN_NM = "unique_column";
        public const string FD_UNIQUE_COLUMN_VAL = "unique_column_value";
        public const string FD_COURIER_COL = "courier_col";
        public const string FD_SAVE_DIR_ORDERED = "file_save_dirs_psv";
        public const string FD_IS_SECONDARY = "is_secondary_file";
        public const string FD_IS_SINGLE_FORMAT = "is_single_format_file";
        public const string FD_REC_TO_JSON_PAIRS_CSV = "rec_to_json_map_pairs";
        public const string FD_SINGLE_FORM_ROWTYPE = "@@";  //this is placeholder for row-type such as FH / PD /BH etc that multi format files have
        public const string FILLER = "@@";  //this is placeholder when some column/logic is not applicable But json def stays consistent

        // 3 flags - card print | letter | PTC entry
        // introduce batch concept

    }

}