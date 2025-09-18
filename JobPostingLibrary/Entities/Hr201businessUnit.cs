using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201businessUnit
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LegacyId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public long? OrganizationUnitId { get; set; }

    public Guid? Hr201industryId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<Hr201employee> Hr201employees { get; set; } = new List<Hr201employee>();

    public virtual ICollection<PlantillaJobMngmnt> PlantillaJobMngmnts { get; set; } = new List<PlantillaJobMngmnt>();

    public virtual ICollection<PlantillaRank> PlantillaRanks { get; set; } = new List<PlantillaRank>();
}
