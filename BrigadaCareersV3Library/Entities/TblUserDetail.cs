using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblUserDetail
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? ContactNo { get; set; }

    public string? EmailAddress { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Guid? Hr201GenderId { get; set; }

    public Guid? Hr201CivilStatus { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreationTime { get; set; }

    public string? Address { get; set; }

    public string? AboutMe { get; set; }

    public string? StreetDetails { get; set; }

    public Guid UserId { get; set; }

    public Guid? UserProfileImageId { get; set; }

    public virtual ICollection<TblCertificate> TblCertificates { get; set; } = new List<TblCertificate>();

    public virtual ICollection<TblEducation> TblEducations { get; set; } = new List<TblEducation>();

    public virtual ICollection<TblUserResume> TblUserResumes { get; set; } = new List<TblUserResume>();

    public virtual ICollection<TblWorkExperience> TblWorkExperiences { get; set; } = new List<TblWorkExperience>();

    public virtual TblAppbinary? UserProfileImage { get; set; }
}
