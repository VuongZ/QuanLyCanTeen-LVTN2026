using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using System;
using System.Threading.Tasks;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhoImportController : ControllerBase
    {
        private readonly KhoImportService _importService;

        public KhoImportController(KhoImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("submit-import")]
        public async Task<IActionResult> SubmitImportTicket([FromBody] CreateImportTicketDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(new { message = "Phiếu nhập từ file Excel không có sản phẩm nào hợp lệ." });
            }

            try
            {
                await _importService.CreateImportTicketAsync(dto);
                return Ok(new { message = "Nhập kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}