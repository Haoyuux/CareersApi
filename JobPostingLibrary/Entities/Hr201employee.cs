using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class Hr201employee
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LegacyId { get; set; }

    public string Firstname { get; set; } = null!;

    public string? Middlename { get; set; }

    public string Lastname { get; set; } = null!;

    public string? SuffixName { get; set; }

    public string? Nickname { get; set; }

    public DateTime Bdate { get; set; }

    public string? BirthPlaceAddressLegacyField { get; set; }

    public string? ProvincialAddressLegacyField { get; set; }

    public string? PresentAddressLegacyField { get; set; }

    public string? Tin { get; set; }

    public string? PhicId { get; set; }

    public string? Sssid { get; set; }

    public string? Hdmfid { get; set; }

    public string? CompanyEmailAddress { get; set; }

    public string? PersonalEmailAddress { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactno { get; set; }

    public string? MobileNo { get; set; }

    public int? Salutation { get; set; }

    public string? Weight { get; set; }

    public string? Height { get; set; }

    public Guid? Hr201businessUnitId { get; set; }

    public Guid? Hr201locationId { get; set; }

    public Guid? Hr201departmentId { get; set; }

    public Guid? Hr201genderId { get; set; }

    public Guid? Hr201civilStatusId { get; set; }

    public Guid? Hr201designationId { get; set; }

    public Guid? Hr201employmentStatId { get; set; }

    public Guid? Hr201bloodTypeId { get; set; }

    public Guid? Hr201birStatId { get; set; }

    public Guid? Hr201religionId { get; set; }

    public Guid? PresentAddressId { get; set; }

    public Guid? ProvincialAddressId { get; set; }

    public Guid? BirthPlaceAddressId { get; set; }

    public Guid? Hr201citizenshipId { get; set; }

    public Guid? BinaryObjectId { get; set; }

    public Guid? RecrtmntApplicantMasterlistId { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public DateTime? DateHired { get; set; }

    public virtual AppBinaryObject? BinaryObject { get; set; }

    public virtual ICollection<Contract> ContractJobOfferFroms { get; set; } = new List<Contract>();

    public virtual ICollection<Contract> ContractLetterFromNavigations { get; set; } = new List<Contract>();

    public virtual Hr201businessUnit? Hr201businessUnit { get; set; }

    public virtual Hr201civilStatus? Hr201civilStatus { get; set; }

    public virtual Hr201gender? Hr201gender { get; set; }

    public virtual Hr201location? Hr201location { get; set; }

    public virtual ICollection<Mrdetail> Mrdetails { get; set; } = new List<Mrdetail>();

    public virtual RecrtmntApplicantMasterlist? RecrtmntApplicantMasterlist { get; set; }

    public virtual ICollection<RecrtmntJobPostingDetail> RecrtmntJobPostingDetails { get; set; } = new List<RecrtmntJobPostingDetail>();
}
