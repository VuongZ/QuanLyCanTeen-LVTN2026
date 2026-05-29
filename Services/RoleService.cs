

using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;
public class RoleService(RoleRepo roleRepo)
{
    //Xử Lý Luôn Điều Kiện 
    public async Task<IEnumerable<NsRole>> GetAllRole()
    {
        return await roleRepo.GetAll();
    }
    
}