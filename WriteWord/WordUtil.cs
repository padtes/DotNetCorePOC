using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using Logging;

namespace WriteWord
{
    public class WordUtil
    {
        private bool templateLoaded;
        private string _templatePath;
        private string _outDir;
        private WordprocessingDocument wordDoc;
        private string docText;

        private const string logProgramName = "WordUtil";

        public WordUtil(string templatePath, string outDir)
        {
            templateLoaded = false;
            _templatePath = templatePath;
            _outDir = outDir;
            if (outDir.EndsWith("\\") == false)
            {
                _outDir = _outDir + "\\";
            }
            LoadTemplate();
        }

        public bool GetCopyForIdTest(string outTestFileName, string testToken, string testValue)
        {
            if (templateLoaded == false)
            {
                return false;
            }

            try
            {
                string outText = docText.Replace(testToken, testValue);

                using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
                //using (StreamWriter sw = new StreamWriter(_outDir + outTestFileName))
                {
                    sw.Write(outText);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "GetCopyForIdTest", 0, ex);
                return false;
            }

            return true;
        }

        private void LoadTemplate()
        {
            try
            {
                wordDoc = WordprocessingDocument.Open(_templatePath, true);

                docText = null;
                using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
                {
                    docText = sr.ReadToEnd();
                }
                templateLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.WriteEx(logProgramName, "LoadTemplate", 0, ex);
            }
        }
    }
}
