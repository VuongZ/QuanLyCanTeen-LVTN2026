using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ xuất hàng
    /// từ kho chi nhánh ra quầy.
    ///
    /// Service chịu trách nhiệm:
    /// - Kiểm tra người thực hiện.
    /// - Kiểm tra lịch làm chính thức.
    /// - Kiểm tra khung giờ được phép xuất.
    /// - Kiểm tra số lượng tồn kho.
    /// - Điều phối tạo phiếu và cập nhật tồn kho.
    ///
    /// Luồng xử lý:
    /// Controller -> Service -> Repository -> Database.
    /// </summary>
    public partial class KhoExportService
    {
public async Task<
            List<FrontStockExportTicketListDto>>
            GetFrontStockExportTicketsAsync(
                int? branchId)
        {
            return await _exportRepo
                .GetFrontStockExportTicketsAsync(
                    branchId
                );
        }

        /// <summary>
        /// Lấy chi tiết một phiếu xuất ra quầy.
        ///
        /// Khi branchId có giá trị,
        /// phiếu phải thuộc đúng chi nhánh đó.
        /// </summary>
        public async Task<
            FrontStockExportTicketDetailDto?>
            GetFrontStockExportTicketDetailAsync(
                int ticketId,
                int? branchId)
        {
            if (ticketId <= 0)
            {
                return null;
            }

            return await _exportRepo
                .GetFrontStockExportTicketDetailAsync(
                    ticketId,
                    branchId
                );
        }

        /// <summary>
        /// Kiểm tra người thực hiện có phải Manager
        /// và thuộc đúng chi nhánh hay không.
        /// </summary>
    }
}

