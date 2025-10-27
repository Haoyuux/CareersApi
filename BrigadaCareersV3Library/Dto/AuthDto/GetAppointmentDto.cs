using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    public class GetAppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public string Events { get; set; }
        public DateTime? ScheduledDateTime { get; set; }
        public string JobTitle { get; set; }
        public int status { get; set; }
        public bool? isConfirmed { get; set; }
        public string Remarks { get; set; }

    }
}
