using BrigadaCareersV3Library.Dto.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class GetUserCertificateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Issuer { get; set; }
        public string Highlights { get; set; }
        public DateTime? DateAchieved { get; set; }
        public CertificateTypeEnum Type { get; set; }
        public string ImageUrl { get; set; }  // ✅ Changed from Byte[] to URL
        public string FileName { get; set; }  // ✅ Add this
    }
}
