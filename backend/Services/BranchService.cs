using LuanVanTotNghiep.backend.Models.Entities;
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
        public async Task<IEnumerable<DmBranch>> GettAllBranchAsync()
        {
            return await _repo.GetAll();
        }
        public async Task<DmBranch?> getBranchByIdAsync(int id)
        {
            var branch = await _repo.GetbyId(id);
            if(branch == null)
            return null;
            return branch;
             
        }
        public async Task AddBranchAsync(DmBranch branch)
        {
            await _repo.Add(branch);
        }
        public async Task UpdateBranchAsync(int id, DmBranch branchInput)
        {
            var existingBranch = await _repo.GetbyId(id);
            if(existingBranch != null)
            {
                existingBranch.Name = branchInput.Name;
                existingBranch.Address = branchInput.Address;
                existingBranch.Latitude = branchInput.Latitude;
                existingBranch.Longitude = branchInput.Longitude;
                await _repo.Update(existingBranch);
            }
        }

        public async Task<bool> DeletebranchAsync(int id)
        {
            var branch = await _repo.GetbyId(id);
            if(branch == null)
            {
                return false;
            }
            await _repo.Delete(branch.Id);
            return true;
        }
    }
}