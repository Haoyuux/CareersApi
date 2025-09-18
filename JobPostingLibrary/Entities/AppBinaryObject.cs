using System;
using System.Collections.Generic;

namespace JobPostingLibrary.Entities;

public partial class AppBinaryObject
{
    public Guid Id { get; set; }

    public int? TenantId { get; set; }

    public string? Description { get; set; }

    public byte[] Bytes { get; set; } = null!;

    public virtual ICollection<Hr201employee> Hr201employees { get; set; } = new List<Hr201employee>();

    public virtual ICollection<RecrtmntApplicantMasterlist> RecrtmntApplicantMasterlists { get; set; } = new List<RecrtmntApplicantMasterlist>();

    public virtual ICollection<RecrtmntJobAdvertisement> RecrtmntJobAdvertisements { get; set; } = new List<RecrtmntJobAdvertisement>();
}
