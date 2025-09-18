using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class PlantillaJobMngmnt
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string? Name { get; set; }

    public string? JobDescription { get; set; }

    public string? EssentialDutiesResponsibility { get; set; }

    public string? Education { get; set; }

    public string? SpecializedKnowledge { get; set; }

    public string? Skills { get; set; }

    public string? Experienced { get; set; }

    public string? ProfessionalCertification { get; set; }

    public string? SpecialSkills { get; set; }

    public string? Training { get; set; }

    public string? WorkingConditions { get; set; }

    public string? PhysicalRequirements { get; set; }

    public int Type { get; set; }

    public Guid? PlantillaJobTitleId { get; set; }

    public Guid? Hr201businessUnitId { get; set; }

    public Guid? Hr201locationId { get; set; }

    public Guid? Hr201departmentId { get; set; }

    public Guid? PlantillaRankId { get; set; }

    public Guid? MrjobSourceId { get; set; }

    public Guid? MrjobTypeId { get; set; }

    public Guid? JobTitleReportToId { get; set; }

    public int? PlantillaJobCategoryId { get; set; }

    public Guid? PlantillaCategoryId { get; set; }

    public Guid? Hr201unitId { get; set; }

    public bool IsDepartmentHead { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public bool IsVisibleToOtherLoc { get; set; }

    public bool InActive { get; set; }

    public virtual Hr201businessUnit? Hr201businessUnit { get; set; }

    public virtual Hr201location? Hr201location { get; set; }

    public virtual ICollection<PlantillaJobMngmnt> InverseJobTitleReportTo { get; set; } = new List<PlantillaJobMngmnt>();

    public virtual PlantillaJobMngmnt? JobTitleReportTo { get; set; }

    public virtual ICollection<Mrdetail> Mrdetails { get; set; } = new List<Mrdetail>();

    public virtual PlantillaJobTitle? PlantillaJobTitle { get; set; }

    public virtual PlantillaRank? PlantillaRank { get; set; }
}
