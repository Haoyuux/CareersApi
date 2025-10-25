using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class UpdateJobOfferStatusDto
    {
        public Guid ContractId { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionRemarks { get; set; }
    }
}
