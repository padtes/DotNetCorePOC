using System;
using System.Collections.Generic;
using System.Text;

namespace WriteDocXML
{
    public class SqlHelper
    {
        public static string GetSelect(string pgSchema, List<ColumnDetail> columns, SystemParam sysParam, string[] progParams)
        {
            StringBuilder sql = new StringBuilder("select ");

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

                string c = GetSqlColSyntax(sysParam, progParams, phCol);
                sql.Append(c);

                if (string.IsNullOrEmpty(phCol.Alias) == false)
                {
                    sql.Append(" as ");
                    sql.Append(phCol.Alias);
                }

                first = false;
            }

            sql.Append(" from ")
                .Append(pgSchema).Append('.')
                .Append(sysParam.DataTableName);

            string whereStr = GetWehere(sysParam, progParams);
            if (string.IsNullOrEmpty(whereStr) == false)
            {
                sql.Append(" where ")
                .Append(whereStr);
            }

            if (string.IsNullOrEmpty(sysParam.DataOrderby) == false)
            {
                sql.Append(" order by ")
                .Append(sysParam.DataOrderby);
            }

            sql.Append(';');

            return sql.ToString();
        }

        private static string GetSqlColSyntax(SystemParam sysParam, string[] progParams, ColumnDetail phCol)
        {
            string retStr = "";
            switch (phCol.SrcType.ToUpper())
            {
                case "CODE":
                    retStr = GetSqlSnippet(progParams, phCol, sysParam);
                    break;
                case "JSON":
                    retStr = sysParam.DataTableJsonCol + "->" + phCol.DbValue;
                    break;
                case "PARAM":
                    retStr = GetParamValue(progParams, phCol);
                    break;
                default:  //"COLUMN" / "SQLFUNCTION" / "const"
                    retStr = phCol.DbValue;
                    break;
            }

            return retStr;
        }

        private static string GetSqlSnippet(string[] progParams, ColumnDetail phCol, SystemParam sysParam)
        {
            if (phCol.DbValue.ToLower() == "row_number")
            {
                return "row_number() over (order by " + sysParam.DataOrderby + ")";
            }
            throw new NotImplementedException("not coded cfuntion " + phCol.DbValue);
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

        private static string GetWehere(SystemParam sysParam, string[] progParams)
        {
            String strWh = new String(sysParam.DataWhere);
            for (int i = 0; i < progParams.Length; i++)
            {
                strWh = strWh.Replace("{{" + i + "}}", progParams[i].Replace("'", "''"));
            }
            if (sysParam.WhereColList != null)
            {
                foreach (var wCol in sysParam.WhereColList)
                {
                    string colSyntax = GetSqlColSyntax(sysParam, progParams, wCol);
                    strWh = strWh.Replace(wCol.Tag, colSyntax);
                }
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
