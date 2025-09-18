using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntApplicantMasterlist
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int Type { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string? ContactNo { get; set; }

    public string EmailAddress { get; set; } = null!;

    public DateTime? DateOfBirth { get; set; }

    public string Address { get; set; } = null!;

    public string? TinNumber { get; set; }

    public string? PhilHealthNum { get; set; }

    public string? Sssnumber { get; set; }

    public string? Hdmfnumber { get; set; }

    public string? Birnumber { get; set; }

    public Guid? Hr201genderId { get; set; }

    public Guid? Hr201religionId { get; set; }

    public Guid? Hr201civilStatusId { get; set; }

    public int BgcheckStatus { get; set; }

    public bool IsInternal { get; set; }

    public bool? IsBuInitiated { get; set; }

    public Guid? UserId { get; set; }

    public Guid? BinaryObjectId { get; set; }

    public int ApplicantStatus { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual AppBinaryObject? BinaryObject { get; set; }

    public virtual Hr201civilStatus? Hr201civilStatus { get; set; }

    public virtual ICollection<Hr201employee> Hr201employees { get; set; } = new List<Hr201employee>();

    public virtual Hr201gender? Hr201gender { get; set; }

    public virtual ICollection<RecrtmntApplcntEducHeader> RecrtmntApplcntEducHeaders { get; set; } = new List<RecrtmntApplcntEducHeader>();

    public virtual ICollection<RecrtmntExperienceHeader> RecrtmntExperienceHeaders { get; set; } = new List<RecrtmntExperienceHeader>();

    public virtual ICollection<RecrtmntJobPostingDetail> RecrtmntJobPostingDetails { get; set; } = new List<RecrtmntJobPostingDetail>();
}
