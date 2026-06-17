using System;
using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class RegisterShiftDto
{
    [Required]
    public int UserId { get; set; } // Ai là người đăng ký?

    [Required]
    public int PeriodId { get; set; } // Đăng ký vào Đợt nào? (Ví dụ: ID đợt 7 vừa tạo)

    [Required]
    public int ShiftId { get; set; } // Đăng ký Ca nào? (Ví dụ: Ca Trùng Hoai)

    [Required]
    public DateOnly WorkDate { get; set; } // Làm vào ngày nào cụ thể? (Ví dụ: 2026-06-22)
}