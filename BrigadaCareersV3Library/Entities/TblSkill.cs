using System;
using System.Collections.Generic;

namespace BrigadaCareersV3Library.Entities;

public partial class TblSkill
{
    public Guid Id { get; set; }

    public Guid UserIdFk { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreationTime { get; set; }

    public virtual TblUserDetail UserIdFkNavigation { get; set; } = null!;
}
