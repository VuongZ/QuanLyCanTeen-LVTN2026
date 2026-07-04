using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class NsUserBankAccount
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? BankName { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankAccountName { get; set; }

    public virtual NsUser User { get; set; } = null!;
}
