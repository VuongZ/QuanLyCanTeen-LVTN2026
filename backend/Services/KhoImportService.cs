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
        private readonly KhoImportRepo _importRepo;

        /// <summary>
        /// Nhận KhoImportRepo thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoImportService(
            KhoImportRepo importRepo)
        {
            _importRepo = importRepo;
        }

        /// <summary>
        /// Tạo phiếu nhập kho mới.
        ///
        /// Khi thành công:
        /// - Tạo phiếu nhập.
        /// - Tạo các dòng chi tiết phiếu.
        /// - Tạo sản phẩm mới nếu cần.
        /// - Cộng số lượng vào tồn kho chi nhánh.
        /// </summary>
    }
}
