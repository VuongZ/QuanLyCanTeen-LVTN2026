namespace LuanVanTotNghiep.DTOs
{
    public class InventoryDto
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        public string? BranchName { get; set; }

        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public int? Quantity { get; set; }

        public string? SupplierName { get; set; }
    }
}