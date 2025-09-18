using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobPostingReqChecklist
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int Status { get; set; }

    public byte[]? AttachedImg { get; set; }

    public DateTime? DateTimeSubmitted { get; set; }

    public string? SubmissionApprovedBy { get; set; }

    public Guid? RecrtmntJobPostingDetailId { get; set; }

    public Guid? RecrtmntRequirementChecklistId { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public string? FileType { get; set; }

    public virtual RecrtmntJobPostingDetail? RecrtmntJobPostingDetail { get; set; }

    public virtual RecrtmntRequirementChecklist? RecrtmntRequirementChecklist { get; set; }
}
