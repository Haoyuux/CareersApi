using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class CreateOrUpdateReqSubmissionDto
    {
        public string UserReqFileBase64 { get; set; }
        public string UserReqFileName { get; set; }
        public string UserReqFileContentType { get; set; }
        public bool RemoveUserReqFile { get; set; } = false;
        public Guid RecrtmntRequirementCheckListId { get; set; }
    }
}
