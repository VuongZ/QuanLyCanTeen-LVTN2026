namespace LuanVanTotNghiep.DTOs
{
    public class ImportItemDto
    {
        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}