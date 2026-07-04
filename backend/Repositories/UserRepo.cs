using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;
public class UserRepo : Repository<NsUser> 
{
    public UserRepo(AppDbContext appContext): base (appContext)
    {
        
    }

    public new async Task<IEnumerable<NsUser>> GetAll()
    {
        return await _dbSet
            .Include(user => user.Branch)
            .Include(user => user.Role)
            .Include(user => user.NsUserBankAccounts)
            .ToListAsync();
    }


    public override async Task<NsUser?> GetbyId(int id)
    {
        return await _dbSet
            .Include(user => user.Branch)
            .Include(user => user.Role)
            .Include(user => user.NsUserBankAccounts)
            .FirstOrDefaultAsync(user=>user.Id==id);
    }
}
