using System;

namespace WriteWord
{
    public class WordUtil
    {
        private bool templateLoaded;
        private string _templatePath;
        private string _outDir;

        public WordUtil(string templatePath, string outDir)
        {
            templateLoaded = false;
            _templatePath = templatePath;
            _outDir = outDir;
        }



    }
}
