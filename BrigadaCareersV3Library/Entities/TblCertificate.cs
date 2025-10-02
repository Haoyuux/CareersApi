using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblCertificate
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public string Issuer { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Highlights { get; set; } = null!;

    public DateTime DateAchieved { get; set; }

    public int CertificateType { get; set; }

    public Guid AttachImgId { get; set; }

    public Guid CertificateTypeId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreationTime { get; set; }

    public virtual TblAppbinary AttachImg { get; set; } = null!;

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
