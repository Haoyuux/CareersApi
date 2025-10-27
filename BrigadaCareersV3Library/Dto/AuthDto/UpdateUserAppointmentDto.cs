using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class UpdateUserAppointmentDto
    {
        public Guid Id { get; set; }
        public string? Remarks { get; set; }
        public int Status { get; set; }
    }
}
