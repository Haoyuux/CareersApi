using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201allowance
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public decimal AllowanceVal { get; set; }

    public int Hr201allowanceTypeId { get; set; }

    public Guid ContractId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual Hr201allowanceType Hr201allowanceType { get; set; } = null!;
}
