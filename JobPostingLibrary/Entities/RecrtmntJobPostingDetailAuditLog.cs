using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobPostingDetailAuditLog
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string Description { get; set; } = null!;

    public Guid? RecrtmntJobPostingDetailId { get; set; }

    public bool IsHiddenLog { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual RecrtmntJobPostingDetail? RecrtmntJobPostingDetail { get; set; }
}
