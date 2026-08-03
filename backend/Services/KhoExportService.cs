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
        /// <summary>
        /// Cho phép chuẩn bị hàng trước giờ bắt đầu ca
        /// tối đa 60 phút.
        /// </summary>
        private const int ExportPreparationMinutes = 60;

        private readonly KhoExportRepo _exportRepo;

        /// <summary>
        /// Nhận KhoExportRepo thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoExportService(
            KhoExportRepo exportRepo)
        {
            _exportRepo = exportRepo;
        }

        /// <summary>
        /// Lấy các ca làm trong ngày hiện tại
        /// mà Manager có thể chọn để xuất hàng.
        /// </summary>
    }
}
