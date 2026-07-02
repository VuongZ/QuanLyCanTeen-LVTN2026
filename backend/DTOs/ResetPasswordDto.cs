using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class ResetPasswordDto
{
    [Required]
    public string Identifier { get; set; } = null!;

    [Required]
    public string Otp { get; set; } = null!;

    [Required]
    [MinLength(4)]
    public string NewPassword { get; set; } = null!;
}
