using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class CreateOrEditCertificateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Issuer { get; set; }
        public string Highlights { get; set; }
        public DateTime DateAchieved { get; set; }
        public int CertificateType { get; set; }

        //file

        public string ProfileImageBase64 { get; set; }
        public string ProfileImageFileName { get; set; }
        public string ProfileImageContentType { get; set; }

    }
}
