using System;
using System.Collections.Generic;
using System.Text;

namespace PanProcessor
{
    public class PanFilesGr
    {
        public PanFilesGr()
        {
            PanSecondaries = new List<PanSecondaryFile>();
        }

        public string MainFileFullPath { get; set; }
        public string MainFileName { get; set; }
        public string MainBizType { get; set; }
        
        public List<PanSecondaryFile> PanSecondaries { get; set; }

        public void AddSecondary(string fullNm, string fileNm, string bizType)
        {
            int nextInd = PanSecondaries.Count + 1;
            PanSecondaries.Add(new PanSecondaryFile() {
                Indx = nextInd,
                FileFullPath = fullNm,
                FileName = fileNm,
                BizType = bizType
            });
        }
    }

    public class PanSecondaryFile
    {
        public int Indx { get; set; }
        public string FileFullPath { get; set; }
        public string FileName { get; set; }
        public string BizType { get; set; }

    }
}
