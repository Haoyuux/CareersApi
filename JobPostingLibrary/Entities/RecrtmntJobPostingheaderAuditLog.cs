using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobPostingheaderAuditLog
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string Description { get; set; } = null!;

    public Guid? RecrtmntJobPostingHeaderId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual RecrtmntJobPostingHeader? RecrtmntJobPostingHeader { get; set; }
}
