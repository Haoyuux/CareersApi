using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.UserDto
{
    public class UserLoginDto
    {
        public string userID { get; set; }
        public string UserName { get; set; }
        public string UserToken { get; set; }
        public string newRefreshToken { get; set; }
        public List<string> UserRole { get; set; }
    }
}
