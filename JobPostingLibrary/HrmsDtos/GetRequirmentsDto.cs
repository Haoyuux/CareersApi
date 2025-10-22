using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JobPostingLibrary.HrmsDtos
{
    public class GetRequirmentsDto
    {
        public Guid Id { get; set; }
        public string CheckListName { get; set; }
        public string FileName { get; set; }
        public string Remarks { get; set; }
        public string DateUpload { get; set; }
        public string Status { get; set; }
        public byte[] filebyte { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
    }
}
