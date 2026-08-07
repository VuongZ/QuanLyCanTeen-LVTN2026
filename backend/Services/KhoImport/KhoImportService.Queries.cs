using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class KhoImportService
    {
        public async Task<List<InventoryImportTicketListDto>>
            GetInventoryImportTicketsAsync(int? branchId)
        {
            return await _importRepo
                .GetInventoryImportTicketsAsync(branchId);
        }

        public async Task<InventoryImportTicketDetailDto?>
            GetInventoryImportTicketDetailAsync(
                int ticketId,
                int? branchId)
        {
            if (ticketId <= 0)
            {
                return null;
            }

            return await _importRepo
                .GetInventoryImportTicketDetailAsync(
                    ticketId,
                    branchId);
        }

        public async Task<List<ProductAdminDto>>
            GetProductsForAdminAsync(bool active)
        {
            return await _importRepo
                .GetProductsForAdminAsync(active);
        }

        public async Task<ProductAdminDto> DeactivateProductAsync(
            int productId,
            int adminUserId,
            string? reason)
        {
            var product = await _importRepo
                .GetProductForStatusChangeAsync(productId)
                ?? throw new KeyNotFoundException(
                    "Không tìm thấy sản phẩm.");

            if (product.IsActive == false)
            {
                return await GetProductAdminDtoAsync(product.Id);
            }

            product.IsActive = false;
            product.InactiveAt = DateTime.Now;
            product.InactiveBy = adminUserId > 0
                ? adminUserId
                : null;
            product.InactiveReason = NormalizeProductReason(reason);

            await _importRepo.SaveChangesAsync();
            return await GetProductAdminDtoAsync(product.Id);
        }

        public async Task<ProductAdminDto> RestoreProductAsync(
            int productId)
        {
            var product = await _importRepo
                .GetProductForStatusChangeAsync(productId)
                ?? throw new KeyNotFoundException(
                    "Không tìm thấy sản phẩm.");

            product.IsActive = true;
            product.InactiveAt = null;
            product.InactiveBy = null;
            product.InactiveReason = null;

            await _importRepo.SaveChangesAsync();
            return await GetProductAdminDtoAsync(product.Id);
        }

        private async Task<ProductAdminDto> GetProductAdminDtoAsync(
            int productId)
        {
            var activeProducts = await _importRepo
                .GetProductsForAdminAsync(true);

            var active = activeProducts
                .FirstOrDefault(product => product.Id == productId);

            if (active != null)
            {
                return active;
            }

            var inactiveProducts = await _importRepo
                .GetProductsForAdminAsync(false);

            return inactiveProducts
                .First(product => product.Id == productId);
        }

        private static string? NormalizeProductReason(string? reason)
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
    }
}