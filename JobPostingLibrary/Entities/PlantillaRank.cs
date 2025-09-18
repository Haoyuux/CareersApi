using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class PlantillaRank
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public decimal MinRate { get; set; }

    public decimal MidRate { get; set; }

    public decimal MaxRate { get; set; }

    public Guid? PlantillaRankLvlId { get; set; }

    public Guid? PlantillaBandId { get; set; }

    public Guid? Hr201locationId { get; set; }

    public int Status { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public Guid? Hr201businessUnitId { get; set; }

    public virtual Hr201businessUnit? Hr201businessUnit { get; set; }

    public virtual Hr201location? Hr201location { get; set; }

    public virtual ICollection<PlantillaJobMngmnt> PlantillaJobMngmnts { get; set; } = new List<PlantillaJobMngmnt>();
}
