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

    public class ProductAdminDto
    {
        public int Id { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? InactiveAt { get; set; }
        public int? InactiveBy { get; set; }
        public string? InactiveReason { get; set; }
        public int TotalInventory { get; set; }
        public int TotalFrontStock { get; set; }
    }

    public class ChangeProductStatusDto
    {
        public string? Reason { get; set; }
    }
}