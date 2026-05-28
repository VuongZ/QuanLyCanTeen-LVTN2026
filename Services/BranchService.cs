using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public class BranchService
    {
        private readonly BranchRepo _repo;
        public BranchService(BranchRepo repo)
        {
            _repo = repo;
        }
        public async Task<List<DmBranch>> GettAllBranchAsync()
        {
            return await _repo.GetAllBranchAsync();
        }
        public async Task<DmBranch?> getBranchByIdAsync(int id)
        {
            var branch = await _repo.GetBranchByIdAsynce(id);
            if(branch == null)
            return null;
            return branch;
             
        }
        public async Task AddBranchAsync(DmBranch branch)
        {
            await _repo.AddBranchAsync(branch);
        }
        public async Task UpdateBranchAsync(int id, DmBranch branchInput)
        {
            var existingBranch = await _repo.GetBranchByIdAsynce(id);
            if(existingBranch != null)
            {
                existingBranch.Name = branchInput.Name;
                existingBranch.Address = branchInput.Address;
                existingBranch.Latitude = branchInput.Latitude;
                existingBranch.Longitude = branchInput.Longitude;
                await _repo.UpdateBranchAsync(existingBranch);
            }
        }

        public async Task<bool> DeletebranchAsync(int id)
        {
            var branch = await _repo.GetBranchByIdAsynce(id);
            if(branch == null)
            {
                return false;
            }
            await _repo.DeleteBranchAsync(branch);
            return true;
        }
    }
}