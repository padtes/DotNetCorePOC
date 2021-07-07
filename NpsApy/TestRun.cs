using Logging;
using DataProcessor;
using System;
using System.Collections.Generic;
using DbOps.Structs;
using System.IO;
using System.Globalization;
using System.Linq;
using DbOps;
using NpsScriban;

namespace NpsApy
{
    public class TestRun
    {
        public static void TestCourierSeq()
        {
            string pgConnection = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simpleuser;";
            string pgSchema = "ventura";

            SequenceGen.UnlockAll(pgConnection, pgSchema);

            string courierCode = "PRF";
            int fixedLen = 5;
            try
            {
                var newSeq = SequenceGen.GetNextSequence(pgConnection, pgSchema, "couriers", courierCode, fixedLen);
                Console.WriteLine("first:" + newSeq);

                newSeq = SequenceGen.GetNextSequence(pgConnection, pgSchema, "couriers", courierCode, fixedLen);
                Console.WriteLine("secondt:" + newSeq);

                courierCode = "PST";
                newSeq = SequenceGen.GetNextSequence(pgConnection, pgSchema, "couriers", courierCode, fixedLen);
                Console.WriteLine("first:" + newSeq);

                newSeq = SequenceGen.GetNextSequence(pgConnection, pgSchema, "couriers", courierCode, fixedLen);
                Console.WriteLine("secondt:" + newSeq);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        public static void TestFileSeq()
        {
            string dirPath = @"C:\Zunk\Lite\work\20210620";
            string subDirPattern = "Test_";
            int maxFilesPerSub = 3; int maxDirExpexcted = 999;

            var newSeq = SequenceGen.GetFileDirWithSeq(dirPath, subDirPattern, maxFilesPerSub, maxDirExpexcted);
            Console.WriteLine(newSeq);
        }
        public static void TestScriban()
        {
            ScribanTest.Test();
        }
        public static void TestWrFile()
        {
            string hex = "";
            string txtFile = @"C:\Zunk\Lite\work\20210620\nps_lite\NPSLite\someImage.txt";
            using (StreamReader sr = new StreamReader(txtFile))
            {
                hex = sr.ReadToEnd();
            }
            var bytes = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();

            File.WriteAllBytes(@"C:\Zunk\Lite\work\20210620\nps_lite\NPSLite\somImage.jpg", bytes);
        }

        public static byte[] StringToByteArray(string hex)
        {
            hex = hex.Substring(2, hex.Length - 2);
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hex));
            }

            byte[] HexAsBytes = new byte[hex.Length / 2];

            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hex.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        public static void TestInp()
        {
            Logger.SetLogFileName(@"C:\\Zunk\\POC_Log.txt");

            string pgConnection = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simpleuser; ";
            string pgSchema = "ventura";
            //string moduleName = "LiteTest";
            string inputFilePathName = @"C:\d\Personal\Learning\reg_test_in\PTGPRN0507202114070521001.TXT";
            string jsonParamFilePath = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\InputDefine.json";
            int jobId = 0;

            Dictionary<string, string> paramsDict = new Dictionary<string, string>();
            PopulateParamsDict(paramsDict);

            string dateAsDir = "20210620";
            FileProcessorLite fileProcessor = new FileProcessorLite(pgConnection, pgSchema);
            FileInfoStruct fileInfoStr = new FileInfoStruct()
            {
                id = 1  //the header Id for saving children
            };

            bool suc = FileProcessorUtil.SaveInputToDB(fileProcessor, fileInfoStr, jobId, inputFilePathName, jsonParamFilePath, paramsDict, dateAsDir);
            if (suc)
                Console.WriteLine("Great Success");
        }

        private static void PopulateParamsDict(Dictionary<string, string> paramsDict)
        {
            paramsDict.Add("systemdir", "c:/users/spadte/source/repos/padtes/DotNetCorePOC/ddl_sql");
            paramsDict.Add("inputdir", "c:/zunk/lite/input");
            paramsDict.Add("workdir", "c:/zunk/lite/work");
            paramsDict.Add("output_par", "nps_lite");
            paramsDict.Add("output_lite", "NPSLite");
            paramsDict.Add("output_apy", "APY");
            //paramsDict.Add("output_duplicate", "nps_lite_copy");
            paramsDict.Add("photo_max_per_dir", "150");
            paramsDict.Add("expect_max_subdir", "9990");
        }

        public static void TestJasonLoad()
        {
            Logger.SetLogFileName(@"C:\\Zunk\\POC_Log.txt");

            // string jsonParamFilePathReg = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\InputDefine.json";
            string jsonParamFilePath = @"C:\Users\spadte\source\repos\padtes\DotNetCorePOC\ddl_sql\lite_input.json";

            Dictionary<string, string> paramsDict = new Dictionary<string, string>();
            PopulateParamsDict(paramsDict);

            JsonInputFileDef jDef = new JsonInputFileDef();
            string pgConnection = "Server=localhost; Port=5433; Database=postgres; User Id=userventura; Password=simpleuser; ";

            FileProcessorUtil.LoadJsonParamFile(pgConnection, "ventura", jsonParamFilePath, jDef, paramsDict);

            Console.WriteLine("Great Success");
        }

    }

}
