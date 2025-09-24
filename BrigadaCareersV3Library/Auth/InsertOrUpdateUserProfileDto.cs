using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Auth
{
    public class InsertOrUpdateUserProfileDto
    {
        public string ProfileImageBase64 { get; set; }
        public string ProfileImageFileName { get; set; }
        public string ProfileImageContentType { get; set; }
        public bool RemoveProfileImage { get; set; } = false;
    }
}
