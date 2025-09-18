using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobPostingDetail
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public string ApplicantNo { get; set; } = null!;

    public int Stage { get; set; }

    public int Status { get; set; }

    public DateTime? ScreeningDateTimeSchedule { get; set; }

    public DateTime? InitialDateTimeSchedule { get; set; }

    public DateTime? ExaminationDateTimeSchedule { get; set; }

    public DateTime? PanelInterviewDateTimeSchedule { get; set; }

    public DateTime? ReqSubDateTimeSched { get; set; }

    public DateTime? OnboardingDateTimeSchedule { get; set; }

    public Guid? RecrtmntJobPostingHeaderId { get; set; }

    public Guid? RecrtmntApplicantMasterlistId { get; set; }

    public Guid? PlantillaJobTitleId { get; set; }

    public Guid? Hr201employeeId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Hr201appointment> Hr201appointments { get; set; } = new List<Hr201appointment>();

    public virtual Hr201employee? Hr201employee { get; set; }

    public virtual PlantillaJobTitle? PlantillaJobTitle { get; set; }

    public virtual RecrtmntApplicantMasterlist? RecrtmntApplicantMasterlist { get; set; }

    public virtual ICollection<RecrtmntJobPostingDetailAuditLog> RecrtmntJobPostingDetailAuditLogs { get; set; } = new List<RecrtmntJobPostingDetailAuditLog>();

    public virtual RecrtmntJobPostingHeader? RecrtmntJobPostingHeader { get; set; }

    public virtual ICollection<RecrtmntJobPostingReqChecklist> RecrtmntJobPostingReqChecklists { get; set; } = new List<RecrtmntJobPostingReqChecklist>();
}
