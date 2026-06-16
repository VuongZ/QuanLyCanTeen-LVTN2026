using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class UpdatePeriodDto
{
    [Required]
    public DateOnly StartDate { get; set; }
    
    [Required]
    public DateOnly EndDate { get; set; }
    
    [Required]
    public string Status { get; set; } // Cho phép đổi thành DRAFT, OPEN, PUBLISHED
}