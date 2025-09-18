using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntApplcntEducHeader
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public Guid? RecrtmntApplicantMasterlistId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<RecrtmntApplcntEducDetail> RecrtmntApplcntEducDetails { get; set; } = new List<RecrtmntApplcntEducDetail>();

    public virtual RecrtmntApplicantMasterlist? RecrtmntApplicantMasterlist { get; set; }
}
