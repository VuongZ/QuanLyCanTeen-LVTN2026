using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(4)]
    public string NewPassword { get; set; } = null!;
}
