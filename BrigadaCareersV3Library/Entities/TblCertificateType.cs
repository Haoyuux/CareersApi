using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblCertificateType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreationTime { get; set; }

    public bool IsDeleted { get; set; }
}
