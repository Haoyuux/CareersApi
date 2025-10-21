using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblAppbinary
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? S3key { get; set; }

    public string FileName { get; set; } = null!;

    public long? FilzeSize { get; set; }

    public int? FileType { get; set; }

    public byte[]? Byte { get; set; }

    public DateTime DateUpload { get; set; }

    public int? TypeEnum { get; set; }

    public bool IsDeleted { get; set; }

    public string? Description { get; set; }

    public DateTime CreationTime { get; set; }

    public virtual ICollection<TblCertificate> TblCertificates { get; set; } = new List<TblCertificate>();

    public virtual ICollection<TblUserRequirement> TblUserRequirements { get; set; } = new List<TblUserRequirement>();

    public virtual ICollection<TblUserResume> TblUserResumes { get; set; } = new List<TblUserResume>();

    public virtual TblUserDetail? User { get; set; }
}
