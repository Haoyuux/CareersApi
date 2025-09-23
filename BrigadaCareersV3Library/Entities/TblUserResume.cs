using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblUserResume
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public Guid AttachmentId { get; set; }

    public DateTime CreationTime { get; set; }

    public bool IsDeleted { get; set; }

    public virtual TblAppbinary Attachment { get; set; } = null!;

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
