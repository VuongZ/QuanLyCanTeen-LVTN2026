using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

// 1. DTO Trả về (Output) - Dùng để hiển thị lên React
public class BranchShiftConfigDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public string DayOfWeek { get; set; } = null!;
    public int? MaxStaff { get; set; }
    public string? ShiftName { get; set; }
}

// 2. DTO Gửi lên (Input) - Dùng khi Manager bấm Tạo/Lưu cấu hình
public class SaveShiftConfigDto
{
    [Required]
    public int ShiftId { get; set; }
    
    [Required]
    public string DayOfWeek { get; set; } = null!;
    
    [Required]
    public int MaxStaff { get; set; }
}