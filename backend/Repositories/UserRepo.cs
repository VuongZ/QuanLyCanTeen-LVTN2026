using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.AspNetCore.Mvc.TagHelpers;
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
            .Where(user =>user.IsDeleted==false)
            .Include(user => user.Branch)
            .Include(user => user.Role)
            .Include(user => user.NsUserBankAccounts)
            .ToListAsync();
    }


    public override async Task<NsUser?> GetbyId(int id)
    {
        return await _dbSet
            .Include(user => user.Branch)
            .Where(user =>user.IsDeleted==false)
            .Include(user => user.Role)
            .Include(user => user.NsUserBankAccounts)
            .FirstOrDefaultAsync(user=>user.Id==id);
    }
    public async Task SoftDelete(int id)
    {
        var user = await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();
    }

    public async Task<bool> Restore(int id)
    {
        var user = await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == true);
        if (user == null)
            return false;

        user.IsDeleted = false;
        user.DeletedAt = null;
        await Context.SaveChangesAsync();
        return true;
    }

    public  async Task<IEnumerable<NsUser>> GetDaXoa()
    {
        return await _dbSet
            .Include(user => user.Branch)
            .Where(user =>user.IsDeleted==true)
            .Include(user => user.Role)
            .Include(user => user.NsUserBankAccounts)
            .ToListAsync();
    }
}
