using LuanVanTotNghiep.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;
public class UserRepo : Repository<NsUser> 
{
    public UserRepo(AppDbContext appContext): base (appContext)
    {
        
    }


    public override async Task<NsUser?> GetbyId(int id)
    {
        return await  _dbSet.FirstOrDefaultAsync(user=>user.Id==id);
    }
}