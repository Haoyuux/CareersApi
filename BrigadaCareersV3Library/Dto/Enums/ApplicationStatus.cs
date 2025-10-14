using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.Enums
{
    public enum ApplicationStatus
    {
        Pending = 0,
        OnProgress = 1,
        Failed = 2,
        Cancelled = 4,
        ForPooling = 5,
        Qualified = 6,
        Hired = 7,
        Rejected = 8
    }
}
