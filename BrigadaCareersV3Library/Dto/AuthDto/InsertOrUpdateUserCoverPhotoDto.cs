using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class InsertOrUpdateUserCoverPhotoDto
    {
        public string CoverImageBase64 { get; set; }
        public string CoverImageFileName { get; set; }
        public string CoverImageContentType { get; set; }
        public bool RemoveCoverImage { get; set; } = false;
    }
}
