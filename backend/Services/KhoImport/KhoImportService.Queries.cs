using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến nhập kho.
    ///
    /// Service chịu trách nhiệm:
    /// - Kiểm tra dữ liệu đầu vào.
    /// - Kiểm tra quy tắc nghiệp vụ.
    /// - Tính tổng tiền phiếu nhập.
    /// - Điều phối các thao tác Repository.
    ///
    /// Luồng xử lý:
    /// Controller -> Service -> Repository -> Database.
    /// </summary>
    public partial class KhoImportService
    {
public async Task<
            List<InventoryImportTicketListDto>>
            GetInventoryImportTicketsAsync(
                int? branchId)
        {
            return await _importRepo
                .GetInventoryImportTicketsAsync(
                    branchId
                );
        }

        /// <summary>
        /// Lấy chi tiết một phiếu nhập kho.
        ///
        /// Khi branchId có giá trị,
        /// phiếu phải thuộc đúng chi nhánh đó.
        /// </summary>
        public async Task<
            InventoryImportTicketDetailDto?>
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
                    branchId
                );
        }

        /// <summary>
        /// Tìm sản phẩm đã có hoặc tạo sản phẩm mới.
        ///
        /// Thứ tự tìm:
        /// 1. ProductId.
        /// 2. ProductCode.
        /// 3. ProductName.
        /// 4. Tạo sản phẩm mới.
        /// </summary>
    }
}

