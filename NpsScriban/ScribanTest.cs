using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace NpsScriban
{
    public class ScribanTest
    {
        public static void Test()
        {
            string scrTemplate1 = "Dear {{ model.fname }} \n {{ model.lname }} ";  //this "model" is hard-coded - no alternative
            string scrTemplate = "Dear {{ model.fname }} \n" +
                "{{ x = model.lname | string.size \n" +
                " if x > 4 \n" +
                "  model.lname | string.slice 0 lenght:4 \n" +
                " else \n" +
                "  model.lname \n" +
                " end \n" +
                "}}";
            string jsonStr = "{ \"fname\" : \"Nitin\", \"lname\" : \"Sadanandani\" }";

            string res = ScribanHandler.Generate("test1", jsonStr, scrTemplate, false, false);
            //Console.WriteLine(res);

            string scrTemplate3 = "Dear {{ model.name | string.capitalize}} has {{ model.gpa | to_double | math.ceil }}, {{ model.cd[0].c015_pan + ',' + model.cd[0].c014_gender }}";
            string scrTemplate4 =
                @"Dear {{ model.name | capitalize }} and {{ to_upper_cat model.name model.cd[0].c014_gender}} 
                {{-if model.cd[0].c014_gender == 'M' 
                    model.cd[0].c033_email
                  else
                    model.cd[0].c033_phone
                  end
                -}}
                ";

            string jsonStr2 = @"{
                    ""name"":""Bob Smith"",
                    ""gpa"":""3.7"",
                    ""cd"": [
                        {
                        ""c015_pan"": ""ITEPS6508G"",
                        ""c066_uid"": """",
                        ""c003_pran"": ""500501273811"",
                        ""c033_email"": ""DBAPU22@GMAIL.COM"",
                        ""c033_phone"": ""630-333-2222"",
                        ""c005_status"": ""V"",
                        ""c014_gender"": ""M"", 
                        ""c027_permanent_address_state_union_terr"": ""11""
                        }
                    ]
                }
            ";

            //res = ScribanHandler.Generate(jsonStr2, scrTemplate4, false, true);
            //Console.WriteLine(res);

            scrTemplate = @"{{ if model.cd[0].c014_gender == 'M'; 'MALE'; else; 'FEMALE'; end; }}";
            res = ScribanHandler.Generate("test2", jsonStr2, scrTemplate, false,true);
            Console.WriteLine(res);

        }
        private static string Generate(string modelStr, string template, bool liquid = false)
        {
            return Generate(JsonConvert.DeserializeObject<ExpandoObject>(modelStr), template, liquid);
        }

        private static string Generate(object model, string template, bool liquid)
        {
            //this parameter name : "model" is what is used for interogating {{ model.name }}
            //
            //logger.write(@"Generating for {template} using {model}");
            if (liquid)
                return ScribanHandler.RenderLiquid(template, new { model });
            else
                return ScribanHandler.Render(template, new { model });
        }

    }
}
