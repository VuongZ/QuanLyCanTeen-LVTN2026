using LuanVanTotNghiep.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace LuanVanTotNghiep.Repositories
{
    public class BranchRepo
    {
        private readonly AppDbContext _context;
        public BranchRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DmBranch>> GetAllBranchAsync()
        {
            return await _context.DmBranches.ToListAsync();
        }
        public async Task<DmBranch?> GetBranchByIdAsynce(int id)
        {
            return await _context.DmBranches.FindAsync(id);
        }

        public async Task AddBranchAsync(DmBranch branch)
        {
            await _context.DmBranches.AddAsync(branch);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBranchAsync(DmBranch branch)
        {
            _context.DmBranches.Update(branch);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBranchAsync(DmBranch branch)
        {
            _context.DmBranches.Remove(branch);
            await _context.SaveChangesAsync();
        }
    }
}