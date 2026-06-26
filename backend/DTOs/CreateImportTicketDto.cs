using System.Collections.Generic;

namespace LuanVanTotNghiep.DTOs
{
    public class CreateImportTicketDto
    {
        public int ManagerId { get; set; }
        public int BranchId { get; set; }
        public int SupplierId { get; set; }
        
        // Danh sách các mặt hàng Manager đã xác nhận trên màn hình
        public List<ImportItemDto> Items { get; set; } = new List<ImportItemDto>();
    }
}