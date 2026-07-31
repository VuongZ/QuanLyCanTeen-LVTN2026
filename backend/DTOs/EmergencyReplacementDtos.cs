using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class ScheduleAbsenceDto
{
    [Required(
        ErrorMessage = "Vui lòng nhập lý do nghỉ hoặc vắng.")]
    [StringLength(
        500,
        MinimumLength = 3,
        ErrorMessage =
            "Lý do phải từ 3 đến 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class EmergencyReplacementDto
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "Phiếu đăng ký thay thế không hợp lệ.")]
    public int ReplacementRegistrationId { get; set; }
}

public class ReplacementCandidateDto
{
    public int RegistrationId { get; set; }

    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? RoleName { get; set; }

    public DateTime RegisteredAt { get; set; }

    public int QueuePosition { get; set; }
}