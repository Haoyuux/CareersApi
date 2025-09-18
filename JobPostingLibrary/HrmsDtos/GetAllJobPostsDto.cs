using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPostingLibrary.HrmsDtos
{
    public class GetAllJobPostsDto
    {
        public Guid JobPostingId { get; set; }
        public Guid? JobTitleId { get; set; }
        public Byte[] JobAdsPic { get; set; }
        public string Location { get; set; }
        public decimal MinRate { get; set; }
        public decimal MaxRate { get; set; }
        public string? JobTitleName { get; set; }
        public string JobDescription { get; set; }
        public string EssentialDuties { get; set; }
        public string Education { get; set; }
        public string Specialized { get; set; }
        public string Skills { get; set; }
        public string Experience { get; set; }
        public string ProfessionalCert { get; set; }
        public string SpecialSkills { get; set; }
        public string Training { get; set; }
        public string WorkingConditions { get; set; }
        public string BussinessUnit { get; set; }
        public DateTime CreationTime { get; set; }
        public string IntroStatement { get; set; }
        public string? LogoInitial { get; set; }
    }
}
