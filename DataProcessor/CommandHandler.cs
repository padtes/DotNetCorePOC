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

    public class StrFormatter : Command
    {
        //param 0 - data
        //param 1 - format: no_fmt, toupper, tolower, totile
        //param 2 - no_trim, trimstart, trimend, trim, singlespace
        public override string Run(string[] pArr, string[] progParams, DataRow dr, out bool isConst)
        {
            isConst = false;
            if (pArr == null || pArr.Length < 2)
                throw new Exception("invalid parameters for dateformat value");

            int argStInd = pArr[0].IndexOf("[");
            int argEndInd = pArr[0].LastIndexOf("]");
            if (argStInd < 1 || argStInd > argEndInd)
            {
                throw new Exception("malformed string param:" + pArr[0]);
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
                    valAsStr = Regex.Replace(valAsStr, "[ ]{2,}", " ", RegexOptions.None);
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
                case "totile":
                    valAsStr = ti.ToTitleCase(valAsStr);
                    break;
                default:
                    break;
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
