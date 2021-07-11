using System;
using System.Collections.Generic;
using System.Text;
using DbOps.Structs;

namespace DbOps
{
    public class SqlHelper
    {
        public static string GetSelect(string pgSchema, List<ColumnDetail> columns, SystemParamReport sysParam, string[] progParams, Dictionary<string, string> paramsDict)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("select ");
            GetSelectColumns(columns, sysParam, progParams, paramsDict, sql);

            sql.Append(" from ")
                .Append(pgSchema).Append('.')
                .Append(sysParam.DataTableName);

            string whereStr = GetWehere(sysParam, progParams, paramsDict);
            sql.Append(" where ")
                .Append(whereStr)
                .Append(" order by ")
                .Append(sysParam.DataOrderby);
            sql.Append(';');

            return sql.ToString();
        }

        public static void GetSelectColumns(List<ColumnDetail> columns, SystemParamReport sysParam, string[] progParams, Dictionary<string, string> paramsDict, StringBuilder sql)
        {
            bool first = true;
            for (int i = 0; i < columns.Count; i++)
            {
                ColumnDetail phCol = columns[i];

                if (phCol.SrcType.ToUpper() == "CFUNCTION")
                    continue; //C Functions are not part of sql select

                if (first == false)
                {
                    sql.Append(", ");
                }

                string c = GetSqlColSyntax(sysParam, progParams, paramsDict, phCol);
                sql.Append(c);

                if (string.IsNullOrEmpty(phCol.Alias) == false)
                {
                    sql.Append(" as ");
                    sql.Append(phCol.Alias);
                }

                first = false;
            }
        }

        private static string GetSqlColSyntax(SystemParamReport sysParam, string[] progParams, Dictionary<string, string> paramsDict, ColumnDetail phCol)
        {
            string retStr = "";
            switch (phCol.SrcType.ToUpper())
            {
                case "CODE":
                    retStr  = GetSqlSnippet(progParams, phCol, sysParam);
                    break;
                case "JSON":
                    retStr = sysParam.DataTableJsonCol + "->"+ phCol.DbValue;
                    break;
                case "PARAM":
                    retStr = GetParamValue(progParams, phCol);
                    break;
                case "SYS_PARAM":
                case "SYSPARAM":
                    retStr = GetDictParamValue(paramsDict, phCol);
                    break;
                default:  //"COLUMN" / "SQLFUNCTION" / "const"
                    retStr = phCol.DbValue;
                    break;
            }

            return retStr;
        }

        private static string GetSqlSnippet(string[] progParams, ColumnDetail phCol, SystemParamReport sysParam)
        {
            if (phCol.DbValue.ToLower() == "row_number")
            {
                if (string.IsNullOrEmpty(sysParam.DataOrderby))
                {
                    return "row_number() over ()";
                }

                return "row_number() over (order by " + sysParam.DataOrderby + ")";
            }
            throw new NotImplementedException("not coded CODE " + phCol.DbValue);
        }

        public static string GetParamValue(string[] progParams, ColumnDetail phCol)
        {
            int indx;
            if (int.TryParse(phCol.DbValue, out indx) == false)
                throw new Exception($"invalid index {phCol.DbValue} for column {phCol.Tag}");

            if (indx < 0 || indx > progParams.Length - 1)
                throw new Exception($"invalid index {phCol.DbValue} for column {phCol.Tag}, max allowed {progParams.Length - 1}");

            return "'" + progParams[indx].Replace("'", "''") + "'";
        }

        public static string GetDictParamValue(Dictionary<string, string> paramsDict, ColumnDetail phCol)
        {
            if (paramsDict.ContainsKey(phCol.DbValue)==false)
                throw new Exception($"invalid sys param name {phCol.DbValue} for column {phCol.Tag}, check system_param record @system");

            return "'" + paramsDict[phCol.DbValue].Replace("'", "''") + "'";
        }

        private static string GetWehere(SystemParamReport sysParam, string[] progParams, Dictionary<string, string> paramsDict)
        {
            String strWh = new String(sysParam.DataWhere);
            for (int i = 0; i < progParams.Length; i++)
            {
                strWh = strWh.Replace("{{" + i + "}}", progParams[i].Replace("'", "''"));
            }
            foreach (var wCol in sysParam.WhereColList)
            {
                string colSyntax = GetSqlColSyntax(sysParam, progParams, paramsDict, wCol);
                strWh = strWh.Replace(wCol.Tag, colSyntax);
            }

            return strWh;
        }

        public static void RemoveCommentedColumns(List<ColumnDetail> columns)
        {
            for (int i = columns.Count - 1; i >= 0; i--)
            {
                if (columns[i].SrcType.StartsWith("#"))
                    columns.RemoveAt(i);
            }
        }

    }
}
