using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadaCareersV3Library.Dto.Enums
{
    public enum MRFCategory
    {
        FullTime = 0,      // Maps to both 0 (Regular) and 2 (Casual)
        PartTime = 1,      // Maps to both 1 (Regular) and 3 (Casual)
        Project_Based = 4,
        Seasonal = 5,
        Fixed_Term = 6,
        OJT_Student = 7,
    }
}
