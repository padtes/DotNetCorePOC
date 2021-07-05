using DbOps.Structs;
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
            string jsonStr = "{ \"fname\" : \"Nitin\", \"lname\" : \"Sadanandani\", \"cd\": [ { \"c014_gender\": \"M\" } ] }";

            ScriptCol scrCol = new ScriptCol() { DestCol= "x_gender_test", ScriptFile="", Script = scrTemplate};
            string sysPath = "";

            string res = ScribanHandler.Generate(sysPath, scrCol, jsonStr, false, false);
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
                    ],
                ""pd"": [
                      {
                        ""p008_pran"": ""500488763407"",
                        ""p054_apy_flag"": ""Y"",
                        ""p005_serial_no"": ""2"",
                        ""p017_subscriber_address_line_1"": ""GAWARIYON KI DHANI EROLAW POST TEJAKA BASSVIA PHULERA"",
                        ""p018_subscriber_address_line_2"": """",
                        ""p019_subscriber_address_line_3"": """",
                        ""p020_subscriber_address_line_4"": ""SAMBHAR LAKE"",
                        ""p021_subscriber_address_state"": ""27""
                      } 
                    ]
                }";

            ScriptCol scrCol4 = new ScriptCol() { DestCol= "x_test1", ScriptFile= "formatted_addr_subscriber.txt", Script =""};  //scrTemplate4
            sysPath = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\";
            res =  ScribanHandler.Generate(sysPath, scrCol4, jsonStr2, false, false); //ScribanHandler.Generate(jsonStr2, scrTemplate4, false, true);
            Console.WriteLine(res);

            scrTemplate = @"{{ if model.cd[0].c014_gender == 'M'; 'MALE'; else; 'FEMALE'; end; }}";
            ScriptCol scrCol2 = new ScriptCol() { DestCol= "x_test2", ScriptFile="", Script =scrTemplate};
            res = ScribanHandler.Generate(sysPath, scrCol, jsonStr2, false, false); //ScribanHandler.Generate("test2", jsonStr2, scrTemplate, false,true);
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
