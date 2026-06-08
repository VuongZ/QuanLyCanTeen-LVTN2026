namespace LuanVanTotNghiep.DTOs;

public class UserPageDataDto
{
    public IEnumerable<UserDto> Users { get; set; } = [];
    public IEnumerable<RoleDto> Roles { get; set; } = [];
    public IEnumerable<BranchDto> Branches { get; set; } = [];
}
