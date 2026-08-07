using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
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

        public async Task<IEnumerable<BranchDto>> GetAllBranchAsync(
            bool includeInactive = false)
        {
            var branches = await _repo.GetAllBranchesAsync(includeInactive);
            return branches.Select(ToDto).ToList();
        }

        // Giữ tương thích với các chỗ cũ đang dùng trong UserController.
        // Chỉ trả về các cơ sở đang hoạt động.
        public async Task<IEnumerable<DmBranch>> GettAllBranchAsync()
        {
            return await _repo.GetAllBranchesAsync(false);
        }

        public async Task<BranchDto?> GetBranchByIdAsync(int id)
        {
            var branch = await _repo.GetbyId(id);
            return branch == null ? null : ToDto(branch);
        }

        public async Task<BranchDto> AddBranchAsync(DmBranch branch)
        {
            branch.Name = (branch.Name ?? string.Empty).Trim();
            branch.Address = branch.Address?.Trim();

            if (string.IsNullOrWhiteSpace(branch.Name))
            {
                throw new ArgumentException("Tên cơ sở không được để trống.");
            }

            branch.IsActive = true;
            branch.InactiveAt = null;
            branch.InactiveBy = null;
            branch.InactiveReason = null;

            await _repo.Add(branch);
            return ToDto(branch);
        }

        public async Task<BranchDto> UpdateBranchAsync(
            int id,
            DmBranch branchInput)
        {
            var existingBranch = await _repo.GetbyId(id)
                ?? throw new KeyNotFoundException("Không tìm thấy cơ sở.");

            existingBranch.Name = (branchInput.Name ?? string.Empty).Trim();
            existingBranch.Address = branchInput.Address?.Trim();
            existingBranch.Latitude = branchInput.Latitude;
            existingBranch.Longitude = branchInput.Longitude;

            if (string.IsNullOrWhiteSpace(existingBranch.Name))
            {
                throw new ArgumentException("Tên cơ sở không được để trống.");
            }

            await _repo.Update(existingBranch);
            return ToDto(existingBranch);
        }

        public async Task<BranchDto> DeactivateBranchAsync(
            int id,
            int adminUserId,
            string? reason)
        {
            var branch = await _repo.GetbyId(id)
                ?? throw new KeyNotFoundException("Không tìm thấy cơ sở.");

            if (!branch.IsActive)
            {
                return ToDto(branch);
            }

            branch.IsActive = false;
            branch.InactiveAt = DateTime.Now;
            branch.InactiveBy = adminUserId > 0 ? adminUserId : null;
            branch.InactiveReason = NormalizeReason(reason);

            await _repo.Update(branch);
            return ToDto(branch);
        }

        public async Task<BranchDto> RestoreBranchAsync(int id)
        {
            var branch = await _repo.GetbyId(id)
                ?? throw new KeyNotFoundException("Không tìm thấy cơ sở.");

            branch.IsActive = true;
            branch.InactiveAt = null;
            branch.InactiveBy = null;
            branch.InactiveReason = null;

            await _repo.Update(branch);
            return ToDto(branch);
        }

        private static string? NormalizeReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }

            var normalized = reason.Trim();
            return normalized.Length <= 255
                ? normalized
                : normalized[..255];
        }

        private static BranchDto ToDto(DmBranch branch)
        {
            return new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                IsActive = branch.IsActive,
                InactiveAt = branch.InactiveAt,
                InactiveBy = branch.InactiveBy,
                InactiveReason = branch.InactiveReason
            };
        }
    }
}
