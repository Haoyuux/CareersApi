using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Dto.AuthDto
{
    // Header DTO - represents the main job posting with its logs
    public class ApplicantJobLogsHeaderDto
    {
        public string JobName { get; set; }

        public int status { get; set; }

        public int jobstatus { get; set; }
        public string MrfCategory { get; set; }
        public string LocationName { get; set; }
        public string BusinessUnitName { get; set; }

        public List<ApplicantJobLogsDto> ApplicantJobLogsDtos { get; set; } = new List<ApplicantJobLogsDto>();
    }

    // Child DTO - represents individual log entries for a job
    public class ApplicantJobLogsDto
    {
        public string JobNameMother { get; set; } 

        public string DescriptionLogs { get; set; }

        public int status { get; set; }

        public DateTime CreationTime { get; set; }
    }
}