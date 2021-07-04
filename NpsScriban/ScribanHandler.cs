using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
        public static string Generate(string scriptName, string modelStr, string template, bool liquid = false, bool useMyCustomFunc = false)
        {
            ExpandoObject model = JsonConvert.DeserializeObject<ExpandoObject>(modelStr);

            if (useMyCustomFunc)
            {
                ScriptObject scriptObj = new MyCustomFunctions();
                scriptObj["model"] = model;  //this parameter name : "model" is what is used for interogating {{ model.name }}
                var context = new TemplateContext();
                context.PushGlobal(scriptObj);
                return Render2(template, new { model }, context);
            }
            //this parameter name : "model" is what is used for interogating {{ model.name }}
            //
            //logger.write(@"Generating for {template} using {model}");
            if (liquid)
                return RenderLiquid(template, new { model });
            else
                return Render(template, new { model });
        }

        public static string Render(string templateStr, object obj = null)
        {
            var template = Template.Parse(templateStr);
            if (template.HasErrors)
                throw new Exception(string.Join("\n", template.Messages.Select(x => $"{x.Message} at {x.Span.ToStringSimple()}")));

            return template.Render(obj, member => LowerFirstCharacter(member.Name));
        }

        public static string Render2(string templateStr, object obj, TemplateContext context)
        {
            var template = Template.Parse(templateStr);
            if (template.HasErrors)
                throw new Exception(string.Join("\n", template.Messages.Select(x => $"{x.Message} at {x.Span.ToStringSimple()}")));

            return template.Render(context);
        }

        public static string RenderLiquid(string templateStr, object obj = null)
        {
            //var lexerOptions = new LexerOptions() { Lang = ScriptLang.Liquid};
            var template = Template.ParseLiquid(templateStr);

            if (template.HasErrors)
                throw new Exception(string.Join("\n", template.Messages.Select(x => $"{x.Message} at {x.Span.ToStringSimple()}")));

            return template.Render(obj, member => LowerFirstCharacter(member.Name));
        }

        private static string LowerFirstCharacter(string value)
        {
            if (value.Length > 1)
                return char.ToLower(value[0]) + value.Substring(1);
            return value;
        }
        
        //public static string Version => typeof(Scriban.Template).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

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
