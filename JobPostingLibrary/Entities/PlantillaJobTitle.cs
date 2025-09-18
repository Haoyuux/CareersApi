using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class PlantillaJobTitle
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public int IndirectCosting { get; set; }

    public int DirectCosting { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<PlantillaJobMngmnt> PlantillaJobMngmnts { get; set; } = new List<PlantillaJobMngmnt>();

    public virtual ICollection<RecrtmntJobPostingDetail> RecrtmntJobPostingDetails { get; set; } = new List<RecrtmntJobPostingDetail>();
}
