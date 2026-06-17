using System;
using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class CreateShiftDto
{
    [Required]
    public string ShiftName { get; set; } = null!;

    public int BranchId { get; set; } // Chi nhánh mà ca này thuộc về

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Required]
    public int MaxStaff { get; set; } // Số người mặc định

    public bool? IsOt { get; set; } // Có phải ca tăng ca không (1 là có, 0 là không)
}