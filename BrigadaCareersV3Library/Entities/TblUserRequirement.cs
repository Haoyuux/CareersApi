using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblUserRequirement
{
    public Guid Id { get; set; }

    public Guid? UseReqId { get; set; }

    public int? Status { get; set; }

    public Guid RecrtmntRequirementChecklistId { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreationTime { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<TblUserDetail> TblUserDetails { get; set; } = new List<TblUserDetail>();

    public virtual TblAppbinary? UseReq { get; set; }
}
