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
    public class KhoImportService
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
        public async Task<int>
            CreateImportTicketAsync(
                CreateImportTicketDto dto)
        {
            // Kiểm tra người thực hiện.
            if (dto.ManagerId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin quản lý."
                );
            }

            // Kiểm tra chi nhánh nhập kho.
            if (dto.BranchId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin chi nhánh."
                );
            }

            // Kiểm tra nhà phân phối.
            if (dto.SupplierId <= 0)
            {
                throw new InvalidOperationException(
                    "Vui lòng chọn nhà cung cấp."
                );
            }

            // Kiểm tra danh sách sản phẩm.
            if (
                dto.Items == null ||
                dto.Items.Count == 0
            )
            {
                throw new InvalidOperationException(
                    "Phiếu nhập không có sản phẩm hợp lệ."
                );
            }

            // Kiểm tra tài khoản người nhập kho.
            var manager =
                await _importRepo.GetUserByIdAsync(
                    dto.ManagerId
                );

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản quản lý."
                );
            }

            // Người thực hiện phải thuộc đúng
            // chi nhánh đang nhập kho.
            if (manager.BranchId != dto.BranchId)
            {
                throw new InvalidOperationException(
                    "Quản lý không thuộc chi nhánh nhập kho."
                );
            }

            // Kiểm tra chi nhánh có tồn tại.
            var branchExists =
                await _importRepo.BranchExistsAsync(
                    dto.BranchId
                );

            if (!branchExists)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy chi nhánh."
                );
            }

            // Kiểm tra nhà phân phối tồn tại
            // và chưa bị xóa mềm.
            var supplierExists =
                await _importRepo.SupplierExistsAsync(
                    dto.SupplierId
                );

            if (!supplierExists)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy nhà cung cấp."
                );
            }

            // Chuẩn hóa mã hóa đơn.
            var invoiceCode =
                string.IsNullOrWhiteSpace(
                    dto.InvoiceCode
                )
                    ? null
                    : dto.InvoiceCode.Trim();

            // Kiểm tra trùng mã hóa đơn
            // trong cùng một nhà phân phối.
            if (
                !string.IsNullOrWhiteSpace(
                    invoiceCode
                )
            )
            {
                var duplicatedInvoice =
                    await _importRepo
                        .ImportInvoiceExistsAsync(
                            dto.SupplierId,
                            invoiceCode
                        );

                if (duplicatedInvoice)
                {
                    throw new InvalidOperationException(
                        "Mã hóa đơn này đã được nhập cho nhà cung cấp đã chọn."
                    );
                }
            }

            // Chỉ giữ lại những dòng có
            // thông tin nhận diện sản phẩm.
            var validItems = dto.Items
                .Where(item =>
                    !string.IsNullOrWhiteSpace(
                        item.ProductName
                    ) ||
                    !string.IsNullOrWhiteSpace(
                        item.ProductCode
                    ) ||
                    item.ProductId > 0
                )
                .ToList();

            if (validItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "Phiếu nhập không có sản phẩm hợp lệ."
                );
            }

            // Kiểm tra số lượng và đơn giá
            // của từng sản phẩm.
            foreach (var item in validItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        $"Số lượng của sản phẩm " +
                        $"'{item.ProductName}' " +
                        "phải lớn hơn 0."
                    );
                }

                if (item.UnitPrice < 0)
                {
                    throw new InvalidOperationException(
                        $"Đơn giá của sản phẩm " +
                        $"'{item.ProductName}' " +
                        "không được âm."
                    );
                }
            }

            // Tính tổng tiền của phiếu nhập.
            var totalAmount = validItems.Sum(
                item =>
                    item.Quantity *
                    item.UnitPrice
            );

            // Toàn bộ quá trình tạo phiếu,
            // chi tiết và cập nhật tồn kho
            // được thực hiện trong một transaction.
            return await _importRepo
                .ExecuteInTransactionAsync(
                    async () =>
                    {
                        // Tạo phiếu nhập kho.
                        var ticket =
                            new KhoImportTicket
                            {
                                ManagerId =
                                    dto.ManagerId,

                                BranchId =
                                    dto.BranchId,

                                SupplierId =
                                    dto.SupplierId,

                                InvoiceCode =
                                    invoiceCode,

                                InvoiceDate =
                                    dto.InvoiceDate
                                        .HasValue
                                        ? DateOnly
                                            .FromDateTime(
                                                dto.InvoiceDate
                                                    .Value
                                            )
                                        : null,

                                ImportDate =
                                    DateTime.Now,

                                TotalAmount =
                                    totalAmount,

                                Note =
                                    string.IsNullOrWhiteSpace(
                                        dto.Note
                                    )
                                        ? null
                                        : dto.Note.Trim()
                            };

                        // Lưu phiếu trước để lấy ID.
                        await _importRepo
                            .AddImportTicketAsync(
                                ticket
                            );

                        // Xử lý từng sản phẩm
                        // trong phiếu nhập.
                        foreach (
                            var item in validItems
                        )
                        {
                            var product =
                                await FindOrCreateProductAsync(
                                    item,
                                    dto.SupplierId
                                );

                            // Lưu đơn vị tại thời điểm nhập.
                            var unitAtTime =
                                !string.IsNullOrWhiteSpace(
                                    item.Unit
                                )
                                    ? item.Unit.Trim()
                                    : product.Unit;

                            // Tạo chi tiết phiếu nhập.
                            var detail =
                                new KhoImportDetail
                                {
                                    ImportId =
                                        ticket.Id,

                                    ProductId =
                                        product.Id,

                                    UnitAtTime =
                                        unitAtTime,

                                    Quantity =
                                        item.Quantity,

                                    UnitPrice =
                                        item.UnitPrice
                                };

                            _importRepo.AddImportDetail(
                                detail
                            );

                            // Tìm tồn kho hiện tại
                            // của sản phẩm tại chi nhánh.
                            var inventory =
                                await _importRepo
                                    .GetBranchInventoryAsync(
                                        dto.BranchId,
                                        product.Id
                                    );

                            if (inventory == null)
                            {
                                // Chưa có tồn kho:
                                // tạo một dòng mới.
                                inventory =
                                    new KhoBranchInventory
                                    {
                                        BranchId =
                                            dto.BranchId,

                                        ProductId =
                                            product.Id,

                                        Quantity =
                                            item.Quantity
                                    };

                                _importRepo
                                    .AddBranchInventory(
                                        inventory
                                    );
                            }
                            else
                            {
                                // Đã có tồn kho:
                                // cộng thêm số lượng nhập.
                                inventory.Quantity =
                                    (inventory.Quantity ??
                                     0) +
                                    item.Quantity;
                            }
                        }

                        // Lưu chi tiết phiếu,
                        // tồn kho và các thay đổi sản phẩm.
                        await _importRepo
                            .SaveChangesAsync();

                        return ticket.Id;
                    }
                );
        }

        /// <summary>
        /// Lấy danh sách lịch sử phiếu nhập kho.
        ///
        /// branchId null:
        /// lấy toàn hệ thống.
        ///
        /// branchId có giá trị:
        /// chỉ lấy một chi nhánh.
        /// </summary>
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
        private async Task<KhoProduct>
            FindOrCreateProductAsync(
                ImportItemDto item,
                int supplierId)
        {
            var productName =
                item.ProductName?.Trim();

            var productCode =
                item.ProductCode?.Trim();

            var unit =
                string.IsNullOrWhiteSpace(
                    item.Unit
                )
                    ? "Cái"
                    : item.Unit.Trim();

            // Ưu tiên tìm sản phẩm theo ID.
            if (item.ProductId > 0)
            {
                var productById =
                    await _importRepo
                        .GetProductByIdAsync(
                            item.ProductId
                        );

                if (productById != null)
                {
                    return productById;
                }
            }

            // Tiếp theo tìm theo mã sản phẩm.
            if (
                !string.IsNullOrWhiteSpace(
                    productCode
                )
            )
            {
                var productByCode =
                    await _importRepo
                        .GetProductByCodeAsync(
                            productCode
                        );

                if (productByCode != null)
                {
                    return productByCode;
                }
            }

            // Tiếp theo tìm theo tên sản phẩm.
            if (
                !string.IsNullOrWhiteSpace(
                    productName
                )
            )
            {
                var productByName =
                    await _importRepo
                        .GetProductByNameAsync(
                            productName
                        );

                if (productByName != null)
                {
                    // Bổ sung mã sản phẩm nếu sản phẩm
                    // cũ chưa có mã.
                    if (
                        string.IsNullOrWhiteSpace(
                            productByName.ProductCode
                        ) &&
                        !string.IsNullOrWhiteSpace(
                            productCode
                        )
                    )
                    {
                        productByName.ProductCode =
                            productCode;
                    }

                    // Bổ sung đơn vị nếu sản phẩm
                    // cũ chưa có đơn vị.
                    if (
                        string.IsNullOrWhiteSpace(
                            productByName.Unit
                        ) &&
                        !string.IsNullOrWhiteSpace(
                            unit
                        )
                    )
                    {
                        productByName.Unit = unit;
                    }

                    return productByName;
                }
            }

            // Sản phẩm mới bắt buộc
            // phải có tên sản phẩm.
            if (
                string.IsNullOrWhiteSpace(
                    productName
                )
            )
            {
                throw new InvalidOperationException(
                    "Sản phẩm mới phải có tên sản phẩm."
                );
            }

            // Tạo sản phẩm mới.
            var newProduct = new KhoProduct
            {
                ProductCode =
                    string.IsNullOrWhiteSpace(
                        productCode
                    )
                        ? null
                        : productCode,

                ProductName =
                    productName,

                Unit = unit,

                SupplierId =
                    supplierId,

                IsActive = true
            };

            return await _importRepo
                .AddProductAsync(newProduct);
        }
    }
}