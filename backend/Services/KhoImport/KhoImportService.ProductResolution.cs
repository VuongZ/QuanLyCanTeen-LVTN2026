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
                    EnsureProductIsActive(productById);
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
                    EnsureProductIsActive(productByCode);
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
                    EnsureProductIsActive(productByName);

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

        private static void EnsureProductIsActive(KhoProduct product)
        {
            if (product.IsActive == false)
            {
                throw new InvalidOperationException(
                    $"Sản phẩm '{product.ProductName}' đã ngừng hoạt động. " +
                    "Admin phải khôi phục sản phẩm trước khi nhập kho.");
            }
        }
    }
}
