using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public class InventoryService
    {
        private readonly InventoryRepo _repo;

        public InventoryService(InventoryRepo repo)
        {
            _repo = repo;
        }

     public async Task<IEnumerable<InventoryDto>> GetReportAsync(int? branchId, string? roleName, int? userBranchId)
{
    // Nếu là Admin -> Cho phép xem toàn cục hoặc theo branchId được chọn
    if (roleName == "ADMIN")
    {
        if (branchId.HasValue && branchId > 0)
            return await _repo.GetInventoryByBranchIdAsync(branchId.Value);
        
        return await _repo.GetAllInventoryAsync();
    }

    // Nếu là Manager hoặc Staff -> Ép buộc lấy đúng BranchId của người đó
    // Dù trên giao diện họ có gửi sai lên thì Backend vẫn chặn lại
    if (userBranchId.HasValue)
    {
        return await _repo.GetInventoryByBranchIdAsync(userBranchId.Value);
    }

    return new List<InventoryDto>();
}
    }
}