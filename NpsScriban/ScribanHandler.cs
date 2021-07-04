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
        public static void Test()
        {
            //OK_Test1();
            Test2();
        }

        private static void Test2()
        {
            //string scrTemplate = "Dear {{ model.name }} has {{ model.cd[0].c015_pan + ',' + model.cd[0].c014_gender }}";
            string scrTemplate =
                @"Dear {{ model.name | capitalize }} and {{ to_upper model.name model.cd[0].c014_gender}} 
                {{-if model.cd[0].c014_gender == 'M' 
                    model.cd[0].c033_email
                  else
                    model.cd[0].c033_phone
                  end
                -}}
                ";

            string jsonStr = @"{
                    ""name"":""ana bana"",
                    ""cd"": [
                        {
                        ""c015_pan"": ""ITEPS6508G"",
                        ""c066_uid"": """",
                        ""c003_pran"": ""500501273811"",
                        ""c033_email"": ""DBAPU22@GMAIL.COM"",
                        ""c033_phone"": ""630-333-2222"",
                        ""c005_status"": ""V"",
                        ""c014_gender"": ""F"", 
                        ""c027_permanent_address_state_union_terr"": ""11""
                        }
                        ]
                        }
                    ";


            string res = ScribanHandler.Generate(jsonStr, scrTemplate, liquid: false, useContext:true);

            Console.WriteLine(res);
        }

        private static void OK_Test1()
        {
            string scrTemplate = "Dear {{ model.name }}";  //this "model" is hard-coded - no alternative
            string jsonStr = "{ \"name\" : \"Nitin Mukesh\" }";

            string res = ScribanHandler.Generate(jsonStr, scrTemplate);

            Console.WriteLine(res);
        }

        private static string Generate(string modelStr, string template, bool liquid = false, bool useContext = false)
        {
            return Generate(JsonConvert.DeserializeObject<ExpandoObject>(modelStr), template, liquid, useContext);
        }

        private static string Generate(object model, string template, bool liquid, bool useContext)
        {
            if (useContext)
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

        public static string ToUpper(string inStr, string pad)
        {
            return inStr.ToUpper() + "[" + pad + "]";
        }

    }
}
