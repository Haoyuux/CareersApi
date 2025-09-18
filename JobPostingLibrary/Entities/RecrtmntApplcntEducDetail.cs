using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntApplcntEducDetail
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? SchoolName { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public Guid? RecrtmntApplcntEducHeaderId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual RecrtmntApplcntEducHeader? RecrtmntApplcntEducHeader { get; set; }
}
