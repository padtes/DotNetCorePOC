using System;
using System.Collections.Generic;
using System.Text;

namespace WriteDocXML
{
    public class StrUtil
    {
        public void AddToIdList(string rec, List<string> idProps)
        {
            string[] props = rec.Split(' ');
            foreach (var aProp in props)
            {
                if (aProp.Contains("Id=\""))
                    idProps.Add(aProp);
            }
        }

        public static string GetNewKeyVal(string oldKeyVal, List<string> usedIds)
        {
            try
            {
                string[] keyVal = oldKeyVal.Split('=');
                string val = keyVal[1];
                int lastQt = val.LastIndexOf('\"');
                val = val.Substring(0, lastQt);

                val = val.Replace("\"", string.Empty);

                if (usedIds.Contains(val) == false)
                {
                    usedIds.Add(val);
                    return oldKeyVal; //-------------------------- use as is
                }

                int output = Convert.ToInt32(val, 16);
                int maxTry = 100;
                do
                {
                    output++;
                    val = output.ToString("X");
                    if (usedIds.Contains(val) == false)
                    {
                        usedIds.Add(val);
                        break;
                    }
                    maxTry--;
                }
                while (maxTry > 0);

                return keyVal[0] + "=" + "\"" + val + "\"";
            }
            catch (Exception ex)
            {
                return oldKeyVal;
            }
            /*
            string val1 = "w14:paraId=\"FFFFFFFA\"";
            List<string> usedIds = new List<string>();
            Console.WriteLine(StrUtil.GetNewKeyVal(val1, usedIds));
            */
        }
    }
}
