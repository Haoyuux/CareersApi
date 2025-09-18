using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Mrdetail
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int Status { get; set; }

    public DateTime? DateTimeStatusUpdate { get; set; }

    public string? Name { get; set; }

    public Guid? MrheaderId { get; set; }

    public Guid? PlantillaJobMngmntId { get; set; }

    public Guid? PlantillaApprovedMasterlistId { get; set; }

    public string? ControlNum { get; set; }

    public int WorkingDayFrom { get; set; }

    public int WorkingDayTo { get; set; }

    public int RestDayFrom { get; set; }

    public int RestDayTo { get; set; }

    public int WorkingHours { get; set; }

    public int? HireReason { get; set; }

    public Guid? PrevEmployeeId { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public DateTime? SeparationDate { get; set; }

    public decimal Allowance { get; set; }

    public string? Remarks { get; set; }

    public bool ExistingMrfsheet { get; set; }

    public int? Hr201allowanceTypeId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public string? OverridePrevEmployeeName { get; set; }

    public bool IsFinalized { get; set; }

    public virtual Hr201allowanceType? Hr201allowanceType { get; set; }

    public virtual PlantillaJobMngmnt? PlantillaJobMngmnt { get; set; }

    public virtual Hr201employee? PrevEmployee { get; set; }

    public virtual ICollection<RecrtmntJobAdvertisement> RecrtmntJobAdvertisements { get; set; } = new List<RecrtmntJobAdvertisement>();

    public virtual ICollection<RecrtmntJobPostingHeader> RecrtmntJobPostingHeaders { get; set; } = new List<RecrtmntJobPostingHeader>();
}
