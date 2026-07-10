namespace LuanVanTotNghiep.DTOs
{
    public class FrontStockExportTicketListDto
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;

        public int? ScheduleId { get; set; }

        public string? ShiftName { get; set; }

        public string? WorkDate { get; set; }

        public string? ShiftTime { get; set; }

        public string ExportDate { get; set; } = string.Empty;

        public int TotalQuantity { get; set; }

        public int ItemCount { get; set; }

        public string? Note { get; set; }
    }

    public class FrontStockExportTicketDetailDto : FrontStockExportTicketListDto
    {
        public List<FrontStockExportTicketItemDto> Items { get; set; } = new();
    }

    public class FrontStockExportTicketItemDto
    {
        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public int Quantity { get; set; }
    }
}