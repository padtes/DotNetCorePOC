using DbOps;
using DbOps.Structs;
using System;
using System.Collections.Generic;
using System.Data;

namespace DataProcessor
{
    public class CsvOutputHandler
    {
        public string GetVal(ColumnDetail columnDetail, DataSet detailRowDS, int rowInd, string[] progParams, Dictionary<string, string> paramsDict, CommandHandler cmdHandler, string textQualifier, string escQualifier)
        {
            string val = columnDetail.DbValue; //default / "CONST"

            switch (columnDetail.SrcType.ToUpper())
            {
                case "PARAM":
                    val = SqlHelper.GetParamValue(progParams, columnDetail).TrimEnd('\'').TrimStart('\'');
                    break;
                case "SYS_PARAM":
                case "SYSPARAM":
                    val = SqlHelper.GetDictParamValue(paramsDict, columnDetail).TrimEnd('\'').TrimStart('\'');
                    break;
                case "CFUNCTION":
                    bool isConst;
                    if (detailRowDS != null && detailRowDS.Tables.Count > 0 && detailRowDS.Tables[0].Rows.Count > rowInd)
                    {
                        val = cmdHandler.Handle(columnDetail.DbValue, progParams, detailRowDS.Tables[0].Rows[rowInd], out isConst);
                        if (isConst)
                        {
                            columnDetail.SrcType = "const";
                            columnDetail.DbValue = val;
                        }
                    }
                    else
                    {
                        val = "";
                    }
                    break;
                case "COLUMN":
                    string valIndxStr = columnDetail.DbValue;
                    DataRow dr = detailRowDS.Tables[0].Rows[rowInd];
                    val = string.Empty;
                    int valIndx;
                    if (int.TryParse(valIndxStr, out valIndx))
                    {
                        if (dr[valIndx] != DBNull.Value)
                            val = Convert.ToString(dr[valIndx]);
                    }
                    else
                    {
                        if (dr[valIndxStr] != DBNull.Value)
                            val = Convert.ToString(dr[valIndxStr]);
                    }
                    break;
                default:
                    break;
            }

            val = GetTxtQualified(val, textQualifier, escQualifier);
            return val;
        }

        public static string GetTxtQualified(string inStr, string textQualifier, string escQualifier)
        {
            if (string.IsNullOrEmpty(textQualifier) == false)
                return textQualifier + inStr.Replace(textQualifier, escQualifier) + textQualifier;
            else
                return inStr;
        }

    }

    public class CsvOutputHdrHandler : CsvOutputHandler
    {
        public string GetHeader(List<ColumnDetail> headerColumns, DataSet detailRowDS, string[] progParams, Dictionary<string, string> paramsDict
            , string delimit, string textQualifier, string escQualifier)
        {
            String hdr = "";
            //var tbl = detailRowDS.Tables[0];
            CommandHandler cmdHandler = new CommandHandler();

            for (int i = 0; i < headerColumns.Count; i++)
            {
                if (headerColumns[i].PrintYN == "n")
                    continue;

                string hdVal = GetVal(headerColumns[i], detailRowDS, 0, progParams, paramsDict, cmdHandler, textQualifier, escQualifier);
                if (i > 0)
                {
                    hdr += delimit;
                }

                hdr += hdVal;
            }

            return hdr;
        }
    }

    public class CsvOutputDetHandler : CsvOutputHandler
    {
        internal string GetDetRow(int iRow, List<ColumnDetail> detailColumns, DataSet ds, string[] progParams, string delimt, string textQualifier, string escQualifier)
        {
            String det = "";
            int cellInd = 0;
            bool isFirst = true;
            bool isConst;
            string val;
            CommandHandler cmdHandler = new CommandHandler();
            DataRow dr = ds.Tables[0].Rows[iRow];

            for (int i = 0; i < detailColumns.Count; i++)
            {
                if (detailColumns[i].PrintYN == "n")
                {
                    if (detailColumns[i].SrcType.ToUpper() != "CFUNCTION")
                    {
                        cellInd++;
                    }
                    continue;
                }

                if (isFirst == false)
                    det += delimt;

                val = string.Empty;

                if (detailColumns[i].SrcType.ToUpper() == "CFUNCTION")
                {
                    val = cmdHandler.Handle(detailColumns[i].DbValue, progParams, dr, out isConst);
                }
                else
                {
                    if (dr[cellInd] != DBNull.Value)
                    {
                        val = Convert.ToString(dr[cellInd]);
                    }
                    cellInd++;
                }

                det += GetTxtQualified(val, textQualifier, escQualifier);
                isFirst = false;
            }
            return det;
        }
    }

}
