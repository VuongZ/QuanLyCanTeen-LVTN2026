namespace LuanVanTotNghiep.DTOs
{
 public class ImportItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; } = null!; // Bổ sung để dễ đối chiếu tên từ Excel
    }
}