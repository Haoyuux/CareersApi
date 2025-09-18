using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntRequirementChecklist
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = null!;

    public int Category { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public bool IsContractual { get; set; }

    public Guid? Hr201employmentStatId { get; set; }

    public virtual ICollection<RecrtmntJobPostingReqChecklist> RecrtmntJobPostingReqChecklists { get; set; } = new List<RecrtmntJobPostingReqChecklist>();
}
