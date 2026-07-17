namespace LuanVanTotNghiep.backend.Models.Entities;

public class LuongSalaryTransfer
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ManagerId { get; set; }
    public int TransferredByUserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int SalaryCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime TransferredAt { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;
    public virtual NsUser Manager { get; set; } = null!;
    public virtual NsUser TransferredByUser { get; set; } = null!;
}
