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
public async Task<int>
            CreateExportTicketAsync(
                CreateExportTicketDto dto)
        {
            // Kiểm tra người thực hiện.
            if (dto.ManagerId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin quản lý."
                );
            }

            // Kiểm tra chi nhánh xuất hàng.
            if (dto.BranchId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin chi nhánh."
                );
            }

            // Bắt buộc chọn lịch làm chính thức.
            if (
                !dto.ScheduleId.HasValue ||
                dto.ScheduleId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Vui lòng chọn ca làm cần xuất hàng ra quầy."
                );
            }

            // Phiếu phải có ít nhất một sản phẩm.
            if (
                dto.Items == null ||
                dto.Items.Count == 0
            )
            {
                throw new InvalidOperationException(
                    "Phiếu xuất không có sản phẩm nào."
                );
            }

            // Kiểm tra Manager và chi nhánh.
            var manager =
                await GetValidManagerAsync(
                    dto.ManagerId,
                    dto.BranchId
                );

            // Kiểm tra lịch làm và khung giờ.
            await ValidateScheduleForExportAsync(
                dto,
                manager
            );

            // Loại bỏ dòng không hợp lệ
            // và gộp sản phẩm bị lặp.
            var validItems = dto.Items
                .Where(item =>
                    item.ProductId > 0 &&
                    item.Quantity > 0
                )
                .GroupBy(item =>
                    item.ProductId
                )
                .Select(group =>
                    new ExportItemDto
                    {
                        ProductId =
                            group.Key,

                        Quantity =
                            group.Sum(item =>
                                item.Quantity
                            )
                    }
                )
                .ToList();

            if (validItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "Danh sách hàng xuất không hợp lệ."
                );
            }

            // Toàn bộ thao tác tạo phiếu,
            // trừ kho và cộng tồn quầy
            // được thực hiện trong transaction.
            return await _exportRepo
                .ExecuteInTransactionAsync(
                    async () =>
                    {
                        // Tạo phiếu xuất.
                        var ticket =
                            new KhoExportTicket
                            {
                                ManagerId =
                                    dto.ManagerId,

                                BranchId =
                                    dto.BranchId,

                                ScheduleId =
                                    dto.ScheduleId,

                                ExportDate =
                                    DateTime.Now,

                                Note =
                                    string.IsNullOrWhiteSpace(
                                        dto.Note
                                    )
                                        ? null
                                        : dto.Note.Trim()
                            };

                        // Lưu phiếu trước để lấy ID.
                        await _exportRepo
                            .AddExportTicketAsync(
                                ticket
                            );

                        // Xử lý từng sản phẩm.
                        foreach (
                            var item in validItems
                        )
                        {
                            // Kiểm tra sản phẩm tồn tại.
                            var product =
                                await _exportRepo
                                    .GetProductByIdAsync(
                                        item.ProductId
                                    );

                            if (product == null)
                            {
                                throw new InvalidOperationException(
                                    $"Không tìm thấy sản phẩm có ID {item.ProductId}."
                                );
                            }

                            // Lấy tồn kho của sản phẩm
                            // tại chi nhánh.
                            var inventory =
                                await _exportRepo
                                    .GetBranchInventoryAsync(
                                        dto.BranchId,
                                        item.ProductId
                                    );

                            var currentWarehouseQuantity =
                                inventory?.Quantity ?? 0;

                            // Kiểm tra đủ số lượng xuất.
                            if (
                                inventory == null ||
                                currentWarehouseQuantity <
                                item.Quantity
                            )
                            {
                                throw new InvalidOperationException(
                                    $"Sản phẩm '{product.ProductName}' không đủ số lượng trong kho. " +
                                    $"Tồn hiện tại: {currentWarehouseQuantity}, " +
                                    $"cần xuất: {item.Quantity}."
                                );
                            }

                            // Tạo chi tiết phiếu xuất.
                            var detail =
                                new KhoExportDetail
                                {
                                    ExportId =
                                        ticket.Id,

                                    ProductId =
                                        item.ProductId,

                                    Quantity =
                                        item.Quantity
                                };

                            _exportRepo.AddExportDetail(
                                detail
                            );

                            // Trừ số lượng khỏi kho chi nhánh.
                            inventory.Quantity =
                                currentWarehouseQuantity -
                                item.Quantity;

                            // Tìm tồn quầy hiện tại.
                            var frontStock =
                                await _exportRepo
                                    .GetBranchFrontStockAsync(
                                        dto.BranchId,
                                        item.ProductId
                                    );

                            if (frontStock == null)
                            {
                                // Chưa có dòng tồn quầy:
                                // tạo mới.
                                frontStock =
                                    new KhoBranchFrontStock
                                    {
                                        BranchId =
                                            dto.BranchId,

                                        ProductId =
                                            item.ProductId,

                                        Quantity =
                                            item.Quantity
                                    };

                                _exportRepo
                                    .AddBranchFrontStock(
                                        frontStock
                                    );
                            }
                            else
                            {
                                // Đã có tồn quầy:
                                // cộng thêm số lượng xuất.
                                frontStock.Quantity =
                                    (frontStock.Quantity ?? 0) +
                                    item.Quantity;
                            }
                        }

                        // Lưu chi tiết phiếu,
                        // tồn kho và tồn quầy.
                        await _exportRepo
                            .SaveChangesAsync();

                        return ticket.Id;
                    }
                );
        }

        /// <summary>
        /// Lấy danh sách lịch sử phiếu xuất ra quầy.
        ///
        /// branchId null:
        /// lấy toàn hệ thống.
        ///
        /// branchId có giá trị:
        /// chỉ lấy một chi nhánh.
        /// </summary>
    }
}

