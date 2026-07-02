using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;
public class RoleRepo : Repository<NsRole>
{
    public RoleRepo(AppDbContext appContext): base (appContext)
    {
    }


    public override async Task<NsRole?> GetbyId(int id)
    {
        return await  _dbSet.FirstOrDefaultAsync(r=>r.Id==id);
    }
}