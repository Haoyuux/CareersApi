using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201allowanceType
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<Hr201allowance> Hr201allowances { get; set; } = new List<Hr201allowance>();

    public virtual ICollection<Mrdetail> Mrdetails { get; set; } = new List<Mrdetail>();
}
