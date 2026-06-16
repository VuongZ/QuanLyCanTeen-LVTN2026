using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class CreatePeriodDto
{
    [Required]
    public int BranchId { get; set; }
    
    [Required]
    public DateOnly StartDate { get; set; }
    
    [Required]
    public DateOnly EndDate { get; set; }
}