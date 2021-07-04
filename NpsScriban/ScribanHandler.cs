using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scriban;
using Scriban.Runtime;
//using System.Reflection;

namespace NpsScriban
{
    public class ScribanHandler
    {
        public static string Generate(string modelStr, string template, bool liquid = false, bool useMyCustomFunc = false)
        {
            return Generate(JsonConvert.DeserializeObject<ExpandoObject>(modelStr), template, liquid, useMyCustomFunc);
        }

        private static string Generate(object model, string template, bool liquid, bool useMyCustomFunc)
        {
            if (useMyCustomFunc)
            {
                ScriptObject scriptObj = new MyCustomFunctions();
                scriptObj["model"] = model;
                var context = new TemplateContext();
                context.PushGlobal(scriptObj);
                return ScribanUtils.Render2(template, new { model }, context);
            }
            //this parameter name : "model" is what is used for interogating {{ model.name }}
            //
            //logger.write(@"Generating for {template} using {model}");
            if (liquid)
                return ScribanUtils.RenderLiquid(template, new { model });
            else
                return ScribanUtils.Render(template, new { model });
        }
    }
    public class MyCustomFunctions : ScriptObject
    {
        public static string Capitalize(string inStr)
        {
            return inStr.Substring(0, 1).ToUpper() + inStr.Substring(1);
        }

        public static string ToUpperCat(string inStr, string catStr)
        {
            return inStr.ToUpper() + "[" + catStr + "]";
        }

        public static double ToDouble(string inStr)
        {
            return Convert.ToDouble(inStr);
        }
    }
}
