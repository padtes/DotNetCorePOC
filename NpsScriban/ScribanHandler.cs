using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using CommonUtil;
using DbOps.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scriban;
using Scriban.Runtime;
//using System.Reflection;

namespace NpsScriban
{
    public class ScribanHandler
    {
        private static Dictionary<string, string> cacheNameScript = new Dictionary<string, string>();
        private static Dictionary<string, Template> cacheNameTemplate = new Dictionary<string, Template>();

        public static string Generate(string sysPath, ScriptCol scrCol, string modelStr, bool liquid = false, bool useMyCustomFunc = false)
        {
            Template template1;
            string colNm = scrCol.DestCol.ToUpper();// as used in ParseTemplate

            if (cacheNameTemplate.ContainsKey(colNm))
                template1 = cacheNameTemplate[colNm];
            else
                template1 = ParseTemplate(sysPath, scrCol);

            ExpandoObject model = JsonConvert.DeserializeObject<ExpandoObject>(modelStr);

            if (useMyCustomFunc)
            {
                ScriptObject scriptObj = new MyCustomFunctions();
                scriptObj["model"] = model;  //this parameter name : "model" is what is used for interogating {{ model.name }}

                var context = new TemplateContext();
                context.PushGlobal(scriptObj);
                //return Render2(templateStr, new { model }, context);
                return template1.Render(context); 
            }
            //this parameter name : "model" is what is used for interogating {{ model.name }}
            //
            //logger.write(@"Generating for {template} using {model}");
            if (liquid)
            {
                //return RenderLiquid(templateStr, new { model });
                return template1.Render(new { model }, member => LowerFirstCharacter(member.Name));
            }
            else
            {
                //return Render(templateStr, new { model });
                return template1.Render(new { model }, member => LowerFirstCharacter(member.Name));
            }
        }

        public static Template ParseTemplate(string sysPath, ScriptCol scrCol)
        {
            string templateStr = scrCol.Script;
            string templateStrOrFn = scrCol.Script;
            if (string.IsNullOrEmpty(scrCol.Script))
            {
                if (string.IsNullOrEmpty(scrCol.ScriptFile))
                    throw new Exception(" no file or script for scripted column " + scrCol.DestCol);
                if (File.Exists(sysPath + scrCol.ScriptFile) == false)
                    throw new Exception("file not found for scripted column " + scrCol.DestCol + "::" + sysPath + scrCol.ScriptFile);

                templateStrOrFn = scrCol.ScriptFile;
                templateStr = File.ReadAllText(sysPath + scrCol.ScriptFile);
            }

            string tmpSName = scrCol.DestCol.ToUpper();

            Template template = Template.Parse(templateStr);
            if (template.HasErrors)
                throw new Exception(string.Join("\n", template.Messages.Select(x => $"{x.Message} at {x.Span.ToStringSimple()}")));

            if (cacheNameScript.ContainsKey(tmpSName) == false)
            {
                cacheNameScript.Add(tmpSName, templateStrOrFn);
                cacheNameTemplate.Add(tmpSName, template);
            }
            else
            {
                string prScript = cacheNameScript[tmpSName];
                if (prScript != templateStrOrFn)
                    throw new Exception("Scripted more than once same column " + scrCol.DestCol + "\n " + prScript + "\n " + templateStrOrFn);
            }
            return template;
        }

        public static string Render(string templateStr, object obj = null, Template template = null)
        {
            if (template == null)
            {
                template = Template.Parse(templateStr);
                if (template.HasErrors)
                    throw new Exception(string.Join("\n", template.Messages.Select(x => $"{x.Message} at {x.Span.ToStringSimple()}")));
            }

            return template.Render(obj, member => LowerFirstCharacter(member.Name));
        }

        public static string Render2(string templateStr, object obj, TemplateContext context)
        {
            Template template = Template.Parse(templateStr);
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
