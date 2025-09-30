using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class CreateOrEditEducationDto
    {
        public Guid Id { get; set; }
        public string SchoolName { get; set; }
        public string EducationLevel { get; set; }
        public string Course { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
