using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblCertificate
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public string Description { get; set; } = null!;

    public DateTime DateAchieved { get; set; }

    public Guid AttachImgId { get; set; }

    public Guid CertificateTypeId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreationTime { get; set; }

    public virtual TblAppbinary CertificateType { get; set; } = null!;

    public virtual TblCertificateType CertificateTypeNavigation { get; set; } = null!;

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
