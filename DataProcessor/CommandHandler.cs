using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataProcessor
{
    public class CommandHandler
    {
        public string Handle(string commandString, string[] progParams, DataRow dr, out bool isConst)
        {
            commandString = commandString.Trim();

            int paramStInd = commandString.IndexOf("(");
            int paramEndInd = commandString.LastIndexOf(")");

            string commandName = commandString;
            string[] pArr = null;
            if (paramStInd > 0)
            {
                if (paramStInd > paramEndInd)
                {
                    throw new Exception("malformed command:" + commandString);
                }

                commandName = commandString.Substring(0, paramStInd).Trim();

                pArr = commandString.Substring(paramStInd + 1, (paramEndInd - paramStInd - 1)).Split(",");  //issue , cannot be part of param string
            }

            Command cmd = GetCommand(commandName);

            return cmd.Run(pArr, progParams, dr, out isConst);
        }

        public Command GetCommand(string commandName)
        {
            if (commandName.Equals("DateFormat", StringComparison.OrdinalIgnoreCase))
                return new DateFormatter();

            if (commandName.Equals("StrFormat", StringComparison.OrdinalIgnoreCase))
                return new StrFormatter();

            if (commandName.Equals("NumFormat", StringComparison.OrdinalIgnoreCase))
                return new NumFormatter();

            if (commandName.Equals("RowCount", StringComparison.OrdinalIgnoreCase))
                return new RowCount();

            if (commandName.Equals("Join", StringComparison.OrdinalIgnoreCase))
                return new Join();

            throw new Exception("Not coded " + commandName);
        }

    }

    public abstract class Command
    {
        public abstract string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst);

        protected void GetArgStEnd(string[] pArr, out int argStInd, out int argEndInd)
        {
            if (pArr == null || pArr.Length < 2)
                throw new Exception("invalid parameters for dateformat value");
            argStInd = pArr[0].IndexOf("[");
            argEndInd = pArr[0].LastIndexOf("]");
            if (argStInd < 1 || argStInd > argEndInd)
            {
                throw new Exception("malformed string param:" + pArr[0]);
            }
        }

        protected string GetResultAsStr(string[] pArr, string[] progParams, DataRow dr, ref bool isConst, int argStInd, int argEndInd, string defaultOnNull)
        {
            string valAsStr;
            string src = pArr[0].Substring(0, argStInd).Trim();
            string valIndxStr = pArr[0].Substring(argStInd + 1, (argEndInd - argStInd - 1));
            try
            {
                if (src.ToLower() == "args")
                {
                    int valIndx = int.Parse(valIndxStr);
                    valAsStr = progParams[valIndx];
                    isConst = true;
                }
                else
                if (src.ToLower() == "dr")
                {
                    int valIndx;
                    if (int.TryParse(valIndxStr, out valIndx))
                    {
                        if (dr[valIndx] == DBNull.Value)
                            valAsStr = defaultOnNull;
                        else
                            valAsStr = Convert.ToString(dr[valIndx]);
                    }
                    else
                    {
                        if (dr[valIndxStr] == DBNull.Value)
                            valAsStr = defaultOnNull;
                        else
                            valAsStr = Convert.ToString(dr[valIndxStr]);
                    }
                }
                else
                {
                    throw new Exception("invalid parameter for dateformat value " + pArr[0] + " use dr or args");
                }
            }
            catch (Exception ed)
            {
                Exception e1 = new Exception(ed.Message + " invalid value " + pArr[0], ed);
                throw e1;
            }

            return valAsStr;
        }


    }

    public class Join : Command
    {
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            throw new Exception("Join.Run Not coded ");
        }
    }

    public class RowCount : Command
    {
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            isConst = false;  //we don't know

            DataTable dt = dr.Table;
            return dt.Rows.Count.ToString();
        }
    }
    public class NumFormatter : Command
    {
        //param 0 - data
        //param 1 - format string 
        //param 2 - default if null
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            isConst = false;
            GetArgStEnd(pArr, out int argStInd, out int argEndInd);

            string def = "";
            if (pArr.Length > 2)
            {
                def = pArr[2].ToLower();
            }
            string valAsStr = GetResultAsStr(pArr, progParams, dr, ref isConst, argStInd, argEndInd, def);

            try
            {
                double num = Convert.ToDouble(valAsStr);
                valAsStr = num.ToString(pArr[1]);
            }
            catch
            {
            }
            return valAsStr;
        }

    }
    public class StrFormatter : Command
    {
        //param 0 - data
        //param 1 - format: no_fmt, toupper, tolower, totitle
        //param 2 - no_trim, trimstart, trimend, trim, singlespace
        //param 3 - isnullOrThis:this:default val
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            isConst = false;
            GetArgStEnd(pArr, out int argStInd, out int argEndInd);

            string def = "";
            string ifThis = "";
            bool hasTransform = false;
            //string transformName - right now not used.
            if (pArr.Length > 3)
            {
                def = pArr[3].ToLower();
                string[] tmpS1 = def.Split(':');
                if (tmpS1.Length >= 3)
                {
                    hasTransform = true;
                    //transformName  = tmpS1[0];
                    ifThis = tmpS1[1];
                    def = tmpS1[2];
                }
            }

            string valAsStr = GetResultAsStr(pArr, progParams, dr, ref isConst, argStInd, argEndInd, def);
            string trimWhat = "no_trim";
            if (pArr.Length > 2)
            {
                trimWhat = pArr[2].ToLower();
            }
            switch (trimWhat)
            {
                case "trim":
                    valAsStr = valAsStr.Trim();
                    break;
                case "trimstart":
                    valAsStr = valAsStr.TrimStart();
                    break;
                case "trimend":
                    valAsStr = valAsStr.TrimEnd();
                    break;
                case "singlespace":
                    //string dbugStr = "*" + valAsStr + "*";
                    valAsStr = Regex.Replace(valAsStr, "[ ]{2,}", " ", RegexOptions.None);
                    valAsStr = valAsStr.Trim();
                    break;
                default:
                    break;
            }
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            switch (pArr[1])
            {
                case "toupper":
                    valAsStr = ti.ToUpper(valAsStr);
                    break;
                case "tolower":
                    valAsStr = ti.ToLower(valAsStr);
                    break;
                case "totitle":
                    valAsStr = ti.ToTitleCase(valAsStr);
                    break;
                default:
                    break;
            }

            //transform
            if (hasTransform)
            {
                //only one transform
                if (valAsStr == ifThis)
                    valAsStr = def;
            }

            return valAsStr;
        }
    }
    public class DateFormatter : Command
    {
        //param 0 - data  - now
        //param 1 - format
        //param 2 - input format such as ddMMyyyy
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            DateTime val = DateTime.Now;
            isConst = false;

            if (pArr[0].ToLower() != "now") // args[n] OR dr[indx] or dr["named"] or const
            {
                int argStInd = pArr[0].IndexOf("[");
                int argEndInd = pArr[0].LastIndexOf("]");

                if (argStInd < 1) //const
                {
                    if (DateTime.TryParse(pArr[0], out val) == false)
                    {
                        //throw new Exception("invalid date constant" + pArr[0]);
                        return pArr[0]; //------------------------------
                    }
                }
                else
                {
                    if (argStInd > argEndInd)
                    {
                        throw new Exception("malformed date param:" + pArr[0]);
                    }

                    string src = pArr[0].Substring(0, argStInd).Trim();
                    string valIndxStr = pArr[0].Substring(argStInd + 1, (argEndInd - argStInd - 1));
                    string valAsStr;
                    try
                    {
                        if (src.ToLower() == "args")
                        {
                            int valIndx = int.Parse(valIndxStr);
                            valAsStr = progParams[valIndx];
                            isConst = true;
                        }
                        else
                        if (src.ToLower() == "dr")
                        {
                            int valIndx;
                            if (int.TryParse(valIndxStr, out valIndx))
                            {
                                valAsStr = Convert.ToString(dr[valIndx]);
                            }
                            else
                            {
                                valAsStr = Convert.ToString(dr[valIndxStr]);
                            }
                        }
                        else
                        {
                            throw new Exception("invalid parameter for dateformat value " + pArr[0] + " use dr or args");
                        }
                    }
                    catch (Exception ed)
                    {
                        Exception e1 = new Exception(ed.Message + " invalid date " + pArr[0], ed);
                        throw e1;
                    }

                    if (pArr.Length <= 2)
                    {
                        if (DateTime.TryParse(valAsStr, out val) == false)
                            // throw new Exception("invalid date constant" + pArr[0]);
                            return valAsStr; //------------------------------
                    }
                    else
                    {
                        if (DateTime.TryParseExact(valAsStr, pArr[2], CultureInfo.InvariantCulture, DateTimeStyles.None, out val) == false) //2 is input format
                            return valAsStr; //------------------------------
                    }
                }
            }

            string myFormat = "dd-MM-yyyy"; //default
            if (pArr.Length >= 2)
                myFormat = pArr[1];

            return val.ToString(myFormat);
        }
    }

}
