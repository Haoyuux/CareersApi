using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblAppbinary
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;

    public byte[] Byte { get; set; } = null!;

    public DateTime DateUpload { get; set; }

    public bool IsDeleted { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreationTime { get; set; }

    public virtual ICollection<TblCertificate> TblCertificates { get; set; } = new List<TblCertificate>();

    public virtual ICollection<TblUserDetail> TblUserDetailCoverPhotoImages { get; set; } = new List<TblUserDetail>();

    public virtual ICollection<TblUserDetail> TblUserDetailResumes { get; set; } = new List<TblUserDetail>();

    public virtual ICollection<TblUserDetail> TblUserDetailUserProfileImages { get; set; } = new List<TblUserDetail>();

    public virtual ICollection<TblUserResume> TblUserResumes { get; set; } = new List<TblUserResume>();
}
