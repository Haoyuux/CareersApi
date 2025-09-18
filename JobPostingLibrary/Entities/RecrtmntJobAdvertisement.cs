using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class RecrtmntJobAdvertisement
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int Status { get; set; }

    public DateTime? DatePosted { get; set; }

    public int SourcingOption { get; set; }

    public Guid? MrdetailId { get; set; }

    public Guid? BinaryObjectId { get; set; }

    public string? IntroStatement { get; set; }

    public DateTime CreationTime { get; set; }

    public long? CreatorUserId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public long? LastModifierUserId { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeleterUserId { get; set; }

    public DateTime? DeletionTime { get; set; }

    public virtual AppBinaryObject? BinaryObject { get; set; }

    public virtual Mrdetail? Mrdetail { get; set; }
}
