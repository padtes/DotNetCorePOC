using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DbOps.Structs
{
    public class SummaryReportByCourier
    {
        public string Courier { get; set; }
        public int HoldCount { get; set; }
        public int PtdCount { get; set; }
        public int TotCount => HoldCount + PtdCount;
    }

    public class SummaryReportFileLine
    {
        public string FileName { get; set; }
        public string FileCat { get; set; }
        public string FileSendDt { get; set; }
        public string FileProcDt { get; set; }
        public int SerNo { get; set; }
        public int FileId { get; set; }

        public List<SummaryReportByCourier> courDetList = new List<SummaryReportByCourier>();
        public int TotCount {
            get {
                int cnt = 0;
                foreach (var crr in courDetList)
                {
                    cnt += crr.TotCount;
                }
                return cnt;
            }
        }
        public int TotPtdCount {
            get {
                int cnt = 0;
                foreach (var crr in courDetList)
                {
                    cnt += crr.PtdCount;
                }
                return cnt;
            }
        }
        public int TotHoldCount {
            get {
                int cnt = 0;
                foreach (var crr in courDetList)
                {
                    cnt += crr.HoldCount;
                }
                return cnt;
            }
        }

        public void AddSummaryCounts(string crrName, int ptdCount, int holdCount)
        {
            bool recFound = false;
            foreach (SummaryReportByCourier crr in courDetList)
            {
                if (crr.Courier == crrName)
                {
                    recFound = true;
                    crr.HoldCount += holdCount;
                    crr.PtdCount += ptdCount;
                }
            }
            if (recFound == false)
            {
                courDetList.Add(new SummaryReportByCourier()
                {
                    Courier = crrName,
                    HoldCount = holdCount,
                    PtdCount = ptdCount
                });
            }
        }

        public string GetPrintLine(char delimit, List<string> crrNames)
        {
            List<string> fPrintLine = new List<string>();
            fPrintLine.Add(FileName);
            fPrintLine.Add(FileCat);
            fPrintLine.Add("'" + FileSendDt);
            fPrintLine.Add("'" + FileProcDt);
            fPrintLine.Add(SerNo.ToString());
            fPrintLine.Add(TotCount.ToString());
            fPrintLine.Add("");
            fPrintLine.Add(TotHoldCount.ToString());

            for (int i = 0; i <crrNames.Count; i++)
            {
                bool hasCrr = false;
                foreach (SummaryReportByCourier det in courDetList)
                {
                    if (det.Courier == crrNames[i])
                    {
                        fPrintLine.Add(det.HoldCount.ToString());
                        fPrintLine.Add(det.PtdCount.ToString());
                        fPrintLine.Add(det.TotCount.ToString());
                        hasCrr = true;
                        break;
                    }
                }
                if (hasCrr == false)
                {
                    fPrintLine.Add("");
                    fPrintLine.Add("");
                    fPrintLine.Add("");
                }
            }

            return string.Join(delimit, fPrintLine);
        }
    }
    public class SummaryReport
    {
        private string _runFor;
        private DateTime RunDate;

        public string RunDtDMY {
            get {
                return RunDate.ToString("dd/MM/yyyy");
            }
        }

        public List<SummaryReportFileLine> FileLines = new List<SummaryReportFileLine>();
        public SummaryReport(string runFor)
        {
            _runFor = runFor;
            if (DateTime.TryParseExact(runFor, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out RunDate) ==false)
            {
                throw new Exception(runFor + " invalid yyyyMMdd date");
            }
        }

        public int TotCount {
            get {
                int cnt = 0;
                foreach (var fl in FileLines)
                {
                    cnt += fl.TotCount;
                }
                return cnt;
            }
        }
        public int TotPtdCount {
            get {
                int cnt = 0;
                foreach (var fl in FileLines)
                {
                    cnt += fl.TotPtdCount;
                }
                return cnt;
            }
        }
        public int TotHoldCount {
            get {
                int cnt = 0;
                foreach (var fl in FileLines)
                {
                    cnt += fl.TotHoldCount;
                }
                return cnt;
            }
        }

        public SummaryReportFileLine AddSummaryReportFileLine(int fileId, int serNo, string fname, string fileCat, string fileDtMdy, DateTime? fileProcDt)
        {
            SummaryReportFileLine fiLine = new SummaryReportFileLine()
            {
                FileId = fileId,
                SerNo = serNo,
                FileName = fname,
                FileCat = fileCat
            };

            if (DateTime.TryParseExact(fileDtMdy, "MMddyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fcrDt))
            {
                fiLine.FileSendDt = fcrDt.ToString("dd/MM/yyyy");
            }
            else
            {
                fiLine.FileSendDt = fileDtMdy;
            }

            if (fileProcDt != null && fileProcDt.HasValue)
                fiLine.FileProcDt = fileProcDt.Value.ToString("dd/MM/yyyy");
            else
                fiLine.FileProcDt ="";

            FileLines.Add(fiLine);

            return fiLine;
        }

        public void GetCouriers(List<string> crrNames)
        {
            foreach (SummaryReportFileLine fl in FileLines)
            {
                foreach (SummaryReportByCourier cr in fl.courDetList)
                {
                    if (crrNames.Contains(cr.Courier) ==false)
                    {
                        crrNames.Add(cr.Courier);
                    }
                }
            }
        }

        public string GetHeader1Dt(bool isEodReport, char delimit)
        {
            string h1 = (isEodReport ? "EOD Dispatch Report: " : "Populate File Report: ");

            return h1 + delimit + "'" + DateTime.Now.ToString("dd/MM/yyyy");// RunDtDMY;
        }

        public string GetHeader2Tot(char delimit)
        {
            string[] hd2 = new string[] { "", "Hold:", TotHoldCount.ToString(), "PTD:", TotPtdCount.ToString(), "Total:", TotCount.ToString() };

            return string.Join(delimit, hd2);
        }

        public string GetHeader3Courier(char delimit, List<string> crrNames)
        {
            List<string> hd3 = new List<string>();
            for (int i = 0; i < 8; i++)
                hd3.Add("");

            for (int i = 0; i <crrNames.Count; i++)
            {
                hd3.Add("");
                hd3.Add(crrNames[i]);
                hd3.Add("");
            }

            return string.Join(delimit, hd3);
        }

        public string GetHeader4Det(char delimit, List<string> crrNames)
        {
            List<string> hd4 = new List<string>();
            hd4.Add("PRAN File Name");
            hd4.Add("File Category");
            hd4.Add("File Send Date");
            hd4.Add("Process Date");
            hd4.Add("Sr No");
            hd4.Add("Total Data");
            hd4.Add("DES");
            hd4.Add("Total Hold");

            for (int i = 0; i <crrNames.Count; i++)
            {
                hd4.Add("Hold");
                hd4.Add("PTD");
                hd4.Add("Total");
            }

            return string.Join(delimit, hd4);
        }


    }
}
