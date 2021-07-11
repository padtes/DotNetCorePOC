using DbOps;
using DbOps.Structs;
using System;
using System.Collections.Generic;
using System.Data;

namespace DataProcessor
{
    public class CsvOutputHandler
    {
        public string GetVal(ColumnDetail columnDetail, DataSet detailRowDS, int rowInd, string[] progParams, CommandHandler cmdHandler)
        {
            string val = columnDetail.DbValue; //default / "CONST"

            switch (columnDetail.SrcType.ToUpper())
            {
                case "PARAM":
                    val = SqlHelper.GetParamValue(progParams, columnDetail).TrimEnd('\'').TrimStart('\'');
                    break;
                case "CFUNCTION":
                    bool isConst;
                    val = cmdHandler.Handle(columnDetail.DbValue, progParams, detailRowDS.Tables[0].Rows[rowInd], out isConst);
                    if (isConst)
                    {
                        columnDetail.SrcType = "const";
                        columnDetail.DbValue = val;
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

            return val;
        }
    }

    public class CsvOutputHdrHandler : CsvOutputHandler
    {
        public string GetHeader(List<ColumnDetail> headerColumns, DataSet detailRowDS, string[] progParams, string delimit)
        {
            String hdr = "";
            //var tbl = detailRowDS.Tables[0];
            CommandHandler cmdHandler = new CommandHandler();

            for (int i = 0; i < headerColumns.Count; i++)
            {
                if (headerColumns[i].PrintYN != "y")
                    continue;
                string hdVal = GetVal(headerColumns[i], detailRowDS, 0, progParams, cmdHandler);
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
        internal string GetDetRow(int iRow, List<ColumnDetail> detailColumns, DataSet ds, string[] progParams, string delimt)
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
                if (detailColumns[i].PrintYN != "y")
                    continue;

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

                det += val;
                isFirst = false;
            }
            return det;
        }
    }

}
