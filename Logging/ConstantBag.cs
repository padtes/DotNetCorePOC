namespace CommonUtil
{
    public class ConstantBag
    {
        //default user for Added By / Updated By
        public const string BATCH_USER = "batch";
        public const string SYSTEM_PARAM = "system";

        //Modules
        public const string MODULE_LITE = "lite";
        public const string MODULE_REG = "reg";

        //db actions
        public const string IGNORED = "IGNORE";
        public const string INSERTED = "INS";
        public const string UPDATED = "UPD";

        //Lite bussiness
        public const string LITE_IN = "lite_inp";
        public const string LITE_OUT_STATUS = "lite_stat";
        public const string LITE_OUT_RESPONSE = "lite_resp";

        //file directions
        public const string DIRECTION_IN = "i";
        public const string DIRECTION_OUT = "o";
        public const string DIRECTION_INTERNAL = "p";

        //file life cycle steps
        public const string FILE_LC_STEP_TODO = "TO-DO";
        public const string FILE_LC_STEP_TO_DB = "DB_DONE";
        public const string FILE_LC_STEP_RESPONSE = "DONE_RESP";

        public const string FILE_NAME_TAG_UNIQUE_COL = "{{unique_column}}";
        //public const string FILE_NAME_TAG_REC_ID = "{{record_id}}";
        public const string FILE_NAME_TAG_COUR_SEQ = "{{courier_seq}}";

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
        // 3 flags - card print | letter | PTC entry
        // introduce batch concept

    }

}