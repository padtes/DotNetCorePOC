using CommonUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string GetSplitSection(string fullName, string splitParams)
        {
            if (string.IsNullOrEmpty(fullName))
                return "";

            string workFN = fullName.Trim();
            workFN = Regex.Replace(workFN, "[ ]{2,}", " ", RegexOptions.None);

            int splitCount; int selectIndex;

            List<int> maxLengths = new List<int>();
            string[] tmpParams = splitParams.Split(',');
            if (tmpParams.Length < 4)
            {
                throw new Exception(" GetSplitSection: invalid arg splitParams. Need min 4 args " + splitParams);
            }

            splitCount = int.Parse(tmpParams[0]);
            selectIndex = int.Parse(tmpParams[1]);

            if (splitCount <= 1)
                return workFN;

            for (int i = 2; i < tmpParams.Length; i++)
            {
                maxLengths.Add(int.Parse(tmpParams[i]));
            }
            List<string> goodParts = new List<string>();
            bool breakNow = false;
            int curLenInd = 0;

            string part1 = workFN;
            string part2 = "";

            while (breakNow == false)
            {
                if (part1.Length <= maxLengths[curLenInd])
                {
                    goodParts.Add(part1);
                    if (part2 == "")
                    {
                        part1 = "";
                        breakNow = true;
                    }
                    else
                    {
                        part1 = part2;
                        part2 = "";
                        curLenInd++;
                    }
                }
                else
                {
                    int tmpSpInx = part1.LastIndexOf(' ');
                    if (tmpSpInx > 1)
                    {
                        part2 = part1.Substring(tmpSpInx).Trim() + " " + part2;
                        part1 = part1.Substring(0, tmpSpInx).Trim();
                    }
                    else
                    {
                        while (goodParts.Count < splitCount - 1)
                            goodParts.Add("");  //push to end

                        part2 = part1 + " " + part2;
                        goodParts.Add(part2.Trim());
                        breakNow = true;
                    }
                }
                if (curLenInd >= maxLengths.Count)
                {
                    goodParts.Add(part1);
                    breakNow = true;
                }
                if (goodParts.Count >= splitCount -1)
                {
                    goodParts.Add(part1);
                    breakNow = true;
                }
            }
            if (selectIndex > goodParts.Count)
                return "";
            else
                return goodParts[selectIndex - 1];
        }


    }
}
