using CommonUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbOps
{
    public class MiscUtil
    {
        public static string GetAwbTranslatedCode(Dictionary<string, string> paramsDict, string courierId)
        {
            string courierAwbKvcsv = paramsDict[ConstantBag.PARAM_COURIER_KVCSV];
            if (string.IsNullOrEmpty(courierAwbKvcsv))
                return paramsDict[ConstantBag.PARAM_PRINTER_CODE3];

            string retMapped = paramsDict[ConstantBag.PARAM_PRINTER_CODE3];
            courierAwbKvcsv = courierAwbKvcsv.Replace(" ", "");
            string[] tmpCsvList = courierAwbKvcsv.Split(',');
            foreach (string item in tmpCsvList)
            {
                string[] kv = item.Split('=');
                if (kv[0].ToUpper() == courierId.ToUpper())
                {
                    retMapped = kv[1];
                    break;
                }
            }

            return retMapped;
        }

    }
}
