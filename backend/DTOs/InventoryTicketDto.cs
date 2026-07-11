namespace LuanVanTotNghiep.DTOs
{
    public class InventoryImportTicketListDto
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string? InvoiceCode { get; set; }

        public string? InvoiceDate { get; set; }

        public string ImportDate { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public int TotalQuantity { get; set; }

        public int ItemCount { get; set; }

        public string? Note { get; set; }
    }

    public class InventoryImportTicketDetailDto : InventoryImportTicketListDto
    {
        public List<InventoryImportTicketItemDto> Items { get; set; } = new();
    }

    public class InventoryImportTicketItemDto
    {
        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}