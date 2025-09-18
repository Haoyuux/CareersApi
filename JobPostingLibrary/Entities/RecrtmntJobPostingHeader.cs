using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobPostingHeader
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public DateTime DueDateTime { get; set; }

    public string? Name { get; set; }

    public int Status { get; set; }

    public Guid? MrdetailId { get; set; }

    public string? ControlNum { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual Mrdetail? Mrdetail { get; set; }

    public virtual ICollection<RecrtmntJobPostingDetail> RecrtmntJobPostingDetails { get; set; } = new List<RecrtmntJobPostingDetail>();

    public virtual ICollection<RecrtmntJobPostingheaderAuditLog> RecrtmntJobPostingheaderAuditLogs { get; set; } = new List<RecrtmntJobPostingheaderAuditLog>();
}
