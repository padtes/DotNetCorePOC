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
