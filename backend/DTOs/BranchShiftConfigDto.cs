namespace LuanVanTotNghiep.DTOs;

public class BranchShiftConfigDto
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public int? ShiftId { get; set; }
    public int? MaxStaff { get; set; }
    
    // Thêm 2 trường này để sau này Admin nhìn vào biết là cấu hình cho Ca nào, Nhánh nào
    public string? BranchName { get; set; }
    public string? ShiftName { get; set; }
}