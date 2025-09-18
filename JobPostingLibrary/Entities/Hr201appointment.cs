using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201appointment
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public DateTime? ScheduledDateTime { get; set; }

    public string? Message { get; set; }

    public int Status { get; set; }

    public Guid? RecrtmntJobPostingDetailId { get; set; }

    public Guid? Hr201appointmentTypeId { get; set; }

    public bool? IsConfirmed { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? PmdConfirmationTime { get; set; }

    public int Stage { get; set; }

    public int Counter { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual RecrtmntJobPostingDetail? RecrtmntJobPostingDetail { get; set; }
}
