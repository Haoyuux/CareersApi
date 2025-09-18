using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201civilStatus
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LegacyId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<Hr201employee> Hr201employees { get; set; } = new List<Hr201employee>();

    public virtual ICollection<RecrtmntApplicantMasterlist> RecrtmntApplicantMasterlists { get; set; } = new List<RecrtmntApplicantMasterlist>();
}
