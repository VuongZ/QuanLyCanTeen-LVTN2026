namespace LuanVanTotNghiep.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }   // tên branch cho tiện
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }     // tên role cho tiện
    public DateOnly? HireDate { get; set; }
}
