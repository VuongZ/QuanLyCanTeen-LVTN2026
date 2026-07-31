namespace LuanVanTotNghiep.backend.Models.Entities;

public class CaShiftDelegation
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ShiftId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int DelegatedByUserId { get; set; }
    public int DelegateUserId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "PENDING";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;
    public virtual CaShift Shift { get; set; } = null!;
    public virtual NsUser DelegatedByUser { get; set; } = null!;
    public virtual NsUser DelegateUser { get; set; } = null!;
    public virtual ICollection<CaShiftDelegationAudit> Audits { get; set; } = [];
}
