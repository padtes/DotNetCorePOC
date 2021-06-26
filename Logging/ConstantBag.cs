namespace CommonUtil
{
    public class ConstantBag
    {
        //default user for Added By / Updated By
        public const string BATCH_USER = "batch";

        //db actions
        public const string IGNORED = "IGNORE";
        public const string INSERTED = "INS";
        public const string UPDATED = "UPD";

        //Lite Modules
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

// 3 flags - card print | letter | PTC entry
// introduce batch concept

    }

}