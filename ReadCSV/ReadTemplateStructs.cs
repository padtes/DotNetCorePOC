using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadCSV
{
    ///there are 2 classes: ReadTemplateHeader and ReadTemplate Details
    ///ReadTemplate will hold header record - template for the file to read
    ///ReadTemplateDet will hold columns mapping by input index to output column name
    ///
    public class ReadTemplateDet
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int InputIndex { get; set; }
        public string OutputColumn { get; set; }
        public string InputColHeader { get; set; }
        public string DataType { get; set; }
        public bool IsManadatory { get; set; }

        private string _LengthRange;
        public string LengthRange
        {
            get { return _LengthRange; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _LengthRange = "";
                    MaxLength = -1; //max
                }
                else if (value.Contains("-")) //- dash: that is a range
                {
                    SetRange(value.Trim());
                }
                else //it is length
                {
                    MaxLength = -1;
                    if (int.TryParse(value.Trim(), out int _maxLength))
                    {
                        MaxLength = _maxLength;
                    }
                }
            }
        }

        private void SetRange(string value)
        {
            string[] range = value.Split('-');
            if (DataType == INT)
            {
                FromInt = int.MinValue;
                ToInt = int.MaxValue;
                int tmp;
                if (int.TryParse(range[0], out tmp))
                {
                    FromInt = tmp;
                }
                if (int.TryParse(range[1], out tmp))
                {
                    ToInt = tmp;
                }
            } //int
            else if (DataType == DATE)
            {
                FromDate = DateTime.MinValue;
                ToDate = DateTime.MaxValue;
                DateTime tmp;
                if (DateTime.TryParse(range[0], out tmp))
                {
                    FromDate = tmp;
                }
                if (DateTime.TryParse(range[1], out tmp))
                {
                    ToDate = tmp;
                }
            } //date
        }

        public int MaxLength { get; set; }
        public int FromInt { get; set; }
        public int ToInt { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public const string STRING = "STRING";
        public const string INT = "INT";
        public const string DATE = "DATE";

        public bool IsValid (string cell, StringBuilder sbEr)
        {
            bool isOk = true;

            if (string.IsNullOrEmpty(cell))
            {
                if (IsManadatory)
                {
                    isOk = false;
                    sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader)
                        .Append(" Missing Mandatory");
                }
                else
                {
                    return true;//don't validate empty cell
                }
            }

            if (DataType == ReadTemplateDet.STRING)
            {
                if (MaxLength > 0 && cell.Length > MaxLength)
                {
                    isOk = false;
                    sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader).Append(' ').Append(cell)
                        .Append(" Max Len ").Append(MaxLength)
                        .Append(" found ").Append(cell.Length);
                }
            }
            else if (DataType == ReadTemplateDet.INT)
            {
                int tmp;
                if (int.TryParse(cell, out tmp) == false)
                {
                    isOk = false;
                    sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader).Append(' ').Append(cell)
                        .Append(" Invalid Int ");
                }
                else if (LengthRange != "")
                {
                    if (tmp > ToInt || tmp < FromInt)
                    {
                        isOk = false;
                        sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader).Append(' ').Append(cell)
                            .Append(" Out of range Int ")
                            .Append(LengthRange);
                    }
                }
            }
            else if (DataType == ReadTemplateDet.DATE)
            {
                DateTime tmp;
                if (DateTime.TryParse(cell, out tmp) == false)
                {
                    isOk = false;
                    sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader).Append(' ').Append(cell)
                        .Append(" Invalid Date ");
                }
                else if (LengthRange != "")
                {
                    if (tmp > ToDate || tmp < FromDate)
                    {
                        isOk = false;
                        sbEr.Append(' ').Append(InputIndex).Append(' ').Append(InputColHeader).Append(' ').Append(cell)
                            .Append(" Out of range Date ")
                            .Append(LengthRange);
                    }
                }
            }

            return isOk;
        }

        public string GetFormattedValue(string cell)
        {
            if (string.IsNullOrEmpty(cell))
            {
                return "''";
            }
            if (DataType == DATE)
            {
                DateTime tmp;
                if (DateTime.TryParse(cell, out tmp))
                {
                    return "'" + tmp.ToString("yyyy/MM/dd") + "'";
                }
            }

            return "'" + cell.Replace("'", "''") + "'";
        }
    }
    public class ReadTemplateHeader
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string TemplateName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<ReadTemplateDet> ColumnList { get; set; }

        public ReadTemplateHeader()
        {
            ColumnList = new List<ReadTemplateDet>();
        }
    }

}
