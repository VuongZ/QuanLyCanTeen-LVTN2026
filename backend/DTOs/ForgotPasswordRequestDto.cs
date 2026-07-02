using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class ForgotPasswordRequestDto
{
    [Required]
    public string Identifier { get; set; } = null!;
}
