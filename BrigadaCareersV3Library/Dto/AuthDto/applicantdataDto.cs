using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class applicantdataDto
    {
        public Guid id { get; set; }
        public Guid jobPostingId { get; set; }
        public Guid jobTitleId { get; set; }
    }
}
