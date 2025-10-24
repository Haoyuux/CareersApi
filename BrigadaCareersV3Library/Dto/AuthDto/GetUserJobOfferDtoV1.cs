using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class GetUserJobOfferDtoV1
    {
        public string JobTitle { get; set; }
        public Guid ContractId { get; set; }
        public byte[]? PdfByte { get; set; }
        public bool? isConfirmed { get; set; }
        public bool? isRejected { get; set; }
        public string RejectionRemarks { get; set; }
        public string StartDate { get; set; }
        public string BusinessUnitName { get; set; }
        public string LocationName { get; set; }
        public string NoLaterThan { get; set; }
        public string MrfCategory { get; set; }
        public string MrfCategoryString { get; set; }
    }
}
