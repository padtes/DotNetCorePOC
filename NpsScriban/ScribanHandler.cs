using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            string scrTemplate = "Dear {{ model.name }} has {{ model.cd[0].c015_pan }}";
            string jsonStr = @"{
                    ""name"":""Tera Mera"",
                    ""cd"": [
                        {
                        ""c015_pan"": ""ITEPS6508G"",
                        ""c066_uid"": """",
                        ""c003_pran"": ""500501273811"",
                        ""c033_email"": ""DBAPU22@GMAIL.COM"",
                        ""c005_status"": ""V"",
                        ""c014_gender"": ""M"", 
                        ""c027_permanent_address_state_union_terr"": ""11""
                        }
                        ]
                        }
                    ";

            string res = ScribanHandler.Generate(jsonStr, scrTemplate);

            Console.WriteLine(res);

        }

        private static void OK_Test1()
        {
            string scrTemplate = "Dear {{ model.name }}";
            string jsonStr = "{ \"name\" : \"Nitin Mukesh\" }";

            string res = ScribanHandler.Generate(jsonStr, scrTemplate);

            Console.WriteLine(res);
        }

        private static string Generate(string modelStr, string template)
        {
            return Generate(JsonConvert.DeserializeObject<ExpandoObject>(modelStr), template);
        }

        private static string Generate(object model, string template)
        {
            //this parameter name : model is what is used for interogating {{ model.name }}
            //
            //logger.write(@"Generating for {template} using {model}");
            return ScribanUtils.Render(template, new { model });
        }
    }
}
