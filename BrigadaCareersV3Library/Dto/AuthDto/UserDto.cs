using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? ContactNo { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public Guid? Hr201GenderId { get; set; }
        public Guid? Hr201CivilStatusId { get; set; }
        public string Gender { get; set; }
        public string CivilStatus { get; set; }
        public string? Address { get; set; }
        public string? AboutMe { get; set; }
        public string? StreetDetails { get; set; }
        public byte[] UserProfileByte { get; set; }
    }
}
