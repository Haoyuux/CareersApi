using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblEducation
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public string EducationLevel { get; set; } = null!;

    public string Course { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreationTime { get; set; }

    public bool IsDeleted { get; set; }

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
