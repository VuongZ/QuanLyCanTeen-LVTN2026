using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoSupplier
{
    public int Id { get; set; }

    public string SupplierName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    public virtual ICollection<KhoProduct> KhoProducts { get; set; } = new List<KhoProduct>();
}
