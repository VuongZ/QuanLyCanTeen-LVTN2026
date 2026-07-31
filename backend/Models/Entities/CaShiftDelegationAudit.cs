namespace LuanVanTotNghiep.backend.Models.Entities;

public class CaShiftDelegationAudit
{
    public int Id { get; set; }
    public int DelegationId { get; set; }
    public int ActorUserId { get; set; }
    public string ActionType { get; set; } = null!;
    public string? Details { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public virtual CaShiftDelegation Delegation { get; set; } = null!;
    public virtual NsUser ActorUser { get; set; } = null!;
}
