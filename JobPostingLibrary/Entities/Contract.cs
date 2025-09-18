using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Contract
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NoLaterThan { get; set; }

    public decimal Salary { get; set; }

    public DateTime Date { get; set; }

    public string? LetterContent { get; set; }

    public int WorkingDayFrom { get; set; }

    public int WorkingDayTo { get; set; }

    public int WorkingHours { get; set; }

    public Guid PlantillaJobTitleId { get; set; }

    public Guid? RecrtmntJobPostDetailId { get; set; }

    public Guid? JobOfferFromId { get; set; }

    public Guid? Hr201designationId { get; set; }

    public Guid? LetterFrom { get; set; }

    public string? RejectionRemarks { get; set; }

    public bool? IsRejected { get; set; }

    public bool? IsCounterOffer { get; set; }

    public int SalaryType { get; set; }

    public bool? IsConfirmedByPmd { get; set; }

    public DateTime? DateAnswered { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public Guid? EmploymentStatId { get; set; }

    public DateTime? ApprovedByGmtime { get; set; }

    public bool IsApprovedByGm { get; set; }

    public string? ApproverBu { get; set; }

    public string? ApproverJobTitle { get; set; }

    public Guid? JobOfferPdf { get; set; }

    public virtual ICollection<Hr201allowance> Hr201allowances { get; set; } = new List<Hr201allowance>();

    public virtual Hr201employee? JobOfferFrom { get; set; }

    public virtual Hr201employee? LetterFromNavigation { get; set; }

    public virtual PlantillaJobTitle PlantillaJobTitle { get; set; } = null!;

    public virtual RecrtmntJobPostingDetail? RecrtmntJobPostDetail { get; set; }
}
