using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblWorkExperience
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public string CompanyName { get; set; } = null!;

    public string CompanyAddress { get; set; } = null!;

    public string JobTitle { get; set; } = null!;

    public string JobDescription { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreationTime { get; set; }

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
