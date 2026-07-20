namespace LuanVanTotNghiep.backend.Models.Entities;

public class CaCheckoutRequestHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual CaCheckoutRequest Request { get; set; } = null!;
    public virtual NsUser? ActorUser { get; set; }
}
