using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    /// <summary>
    /// Cung cấp các API liên quan đến nhập kho.
    ///
    /// Controller chịu trách nhiệm:
    /// - Xác thực và phân quyền người dùng.
    /// - Đọc thông tin người dùng từ JWT.
    /// - Nhận dữ liệu từ request.
    /// - Gọi KhoImportService xử lý nghiệp vụ.
    /// - Trả kết quả HTTP cho Frontend.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KhoImportController : ControllerBase
    {
        private readonly KhoImportService _importService;
        private readonly InvoiceOcrService _invoiceOcrService;

        /// <summary>
        /// Nhận các Service thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoImportController(
            KhoImportService importService,
            InvoiceOcrService invoiceOcrService)
        {
            _importService = importService;
            _invoiceOcrService = invoiceOcrService;
        }

        /// <summary>
        /// Tạo phiếu nhập kho.
        ///
        /// Chỉ Manager được phép thực hiện nhập kho.
        ///
        /// ManagerId và BranchId được lấy từ JWT,
        /// không sử dụng giá trị do Frontend gửi lên.
        /// </summary>
        [HttpPost("submit-import")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult>
            SubmitImportTicket(
                [FromBody] CreateImportTicketDto dto)
        {
            // Kiểm tra danh sách sản phẩm.
            if (
                dto.Items == null ||
                dto.Items.Count == 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "Phiếu nhập không có sản phẩm nào hợp lệ."
                });
            }

            // Lấy ID người dùng đăng nhập từ JWT.
            var userIdString = GetClaimValue(
                ClaimTypes.NameIdentifier,
                "UserId",
                "userId",
                "id"
            );

            if (
                !int.TryParse(
                    userIdString,
                    out var userId
                ) ||
                userId <= 0
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không xác định được người dùng từ token."
                });
            }

            // Lấy chi nhánh của Manager từ JWT.
            var branchIdString = GetClaimValue(
                "BranchId",
                "branchId",
                "branch_id"
            );

            if (
                !int.TryParse(
                    branchIdString,
                    out var branchId
                ) ||
                branchId <= 0
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin chi nhánh trong token."
                });
            }

            // Ghi đè thông tin Frontend gửi lên
            // bằng dữ liệu đáng tin cậy trong JWT.
            dto.ManagerId = userId;
            dto.BranchId = branchId;

            try
            {
                var ticketId =
                    await _importService
                        .CreateImportTicketAsync(dto);

                return Ok(new
                {
                    message =
                        "Nhập kho thành công!",

                    importTicketId =
                        ticketId
                });
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi nghiệp vụ như:
                // - Trùng mã hóa đơn.
                // - Nhà phân phối không tồn tại.
                // - Số lượng hoặc đơn giá không hợp lệ.
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Ghi chi tiết lỗi tại Backend.
                Console.WriteLine(
                    $"Lỗi tạo phiếu nhập kho: {ex}"
                );

                // Không trả chi tiết lỗi nội bộ
                // cho phía Frontend.
                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi tạo phiếu nhập kho."
                });
            }
        }

        /// <summary>
        /// Gửi ảnh hóa đơn lên hệ thống OCR.
        ///
        /// Chỉ Manager được sử dụng chức năng
        /// nhận diện hóa đơn nhập kho.
        /// </summary>
        [HttpPost("parse-invoice-image")]
        [Authorize(Roles = "MANAGER")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult>
            ParseInvoiceImage(IFormFile file)
        {
            // Kiểm tra file trước khi gọi OCR.
            if (
                file == null ||
                file.Length == 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "Chưa chọn ảnh hóa đơn."
                });
            }

            try
            {
                var result =
                    await _invoiceOcrService
                        .ParseInvoiceImageAsync(file);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi OCR hóa đơn: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi xử lý ảnh hóa đơn."
                });
            }
        }

        /// <summary>
        /// Lấy danh sách lịch sử phiếu nhập kho.
        ///
        /// Admin:
        /// - Không truyền branchId: xem toàn hệ thống.
        /// - Có branchId: xem một chi nhánh.
        ///
        /// Manager:
        /// - Chỉ xem phiếu thuộc chi nhánh trong JWT.
        /// </summary>
        [HttpGet("inventory-tickets")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetInventoryImportTickets(
                [FromQuery] int? branchId)
        {
            try
            {
                var finalBranchId =
                    ResolveBranchIdForQuery(
                        branchId
                    );

                // Giá trị -1 nghĩa là người dùng
                // không có BranchId hợp lệ trong token.
                if (finalBranchId == -1)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Không tìm thấy thông tin chi nhánh trong token."
                    });
                }

                // Giá trị 0 nghĩa là Admin
                // đang xem toàn hệ thống.
                int? filterBranchId =
                    finalBranchId == 0
                        ? null
                        : finalBranchId;

                var data =
                    await _importService
                        .GetInventoryImportTicketsAsync(
                            filterBranchId
                        );

                return Ok(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi lấy lịch sử phiếu nhập: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy danh sách phiếu nhập kho."
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết một phiếu nhập kho.
        ///
        /// Manager chỉ có thể xem phiếu
        /// thuộc chi nhánh trong JWT.
        /// </summary>
        [HttpGet("inventory-tickets/{id:int}")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetInventoryImportTicketDetail(
                int id,
                [FromQuery] int? branchId)
        {
            // Kiểm tra ID phiếu.
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã phiếu nhập kho không hợp lệ."
                });
            }

            try
            {
                var finalBranchId =
                    ResolveBranchIdForQuery(
                        branchId
                    );

                if (finalBranchId == -1)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Không tìm thấy thông tin chi nhánh trong token."
                    });
                }

                int? filterBranchId =
                    finalBranchId == 0
                        ? null
                        : finalBranchId;

                var data =
                    await _importService
                        .GetInventoryImportTicketDetailAsync(
                            id,
                            filterBranchId
                        );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy phiếu nhập kho."
                    });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi lấy chi tiết phiếu nhập: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy chi tiết phiếu nhập kho."
                });
            }
        }

        /// <summary>
        /// Xác định chi nhánh được phép truy vấn.
        ///
        /// Kết quả:
        /// - 0: Admin xem toàn hệ thống.
        /// - Số dương: lọc theo chi nhánh.
        /// - -1: không tìm thấy BranchId hợp lệ.
        /// </summary>
        private int ResolveBranchIdForQuery(
            int? requestedBranchId)
        {
            var role = GetClaimValue(
                ClaimTypes.Role,
                "role",
                "Role"
            )?.Trim().ToUpperInvariant();

            var isAdmin =
                role == "ADMIN" ||
                role == "QUẢN TRỊ" ||
                role == "QUAN TRI";

            // Admin được quyền chọn chi nhánh
            // hoặc xem toàn hệ thống.
            if (isAdmin)
            {
                return
                    requestedBranchId.HasValue &&
                    requestedBranchId.Value > 0
                        ? requestedBranchId.Value
                        : 0;
            }

            // Manager không sử dụng branchId
            // do Frontend gửi lên.
            //
            // Luôn lấy BranchId trong JWT.
            var tokenBranchIdString =
                GetClaimValue(
                    "BranchId",
                    "branchId",
                    "branch_id"
                );

            if (
                !int.TryParse(
                    tokenBranchIdString,
                    out var tokenBranchId
                ) ||
                tokenBranchId <= 0
            )
            {
                return -1;
            }

            return tokenBranchId;
        }

        /// <summary>
        /// Tìm giá trị claim theo nhiều
        /// tên thuộc tính khác nhau.
        /// </summary>
        private string? GetClaimValue(
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims
                    .FirstOrDefault(claim =>
                        claim.Type == claimType
                    )
                    ?.Value;

                if (
                    !string.IsNullOrWhiteSpace(
                        value
                    )
                )
                {
                    return value;
                }
            }

            return null;
        }
    }
}