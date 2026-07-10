namespace LuanVanTotNghiep.DTOs
{
    public class ClosingShiftInfoDto
    {
        public int ScheduleId { get; set; }
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public string WorkDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool AlreadyReported { get; set; }
    }

    public class ClosingFrontStockItemDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public int SystemCount { get; set; }
        public int ActualCount { get; set; }
    }

    public class SubmitShiftClosingDto
    {
        public int ScheduleId { get; set; }
        public string? Note { get; set; }
        public List<SubmitShiftClosingItemDto> Items { get; set; } = new();
    }

    public class SubmitShiftClosingItemDto
    {
        public int ProductId { get; set; }
        public int ActualCount { get; set; }
    }

    public class ShiftClosingReportListDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public int? ScheduleId { get; set; }
        public string? ShiftName { get; set; }
        public string? WorkDate { get; set; }
        public string ReportDate { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TotalSystemCount { get; set; }
        public int TotalActualCount { get; set; }
        public int TotalDifference { get; set; }
        public string? Note { get; set; }
    }

    public class ShiftClosingReportDetailDto : ShiftClosingReportListDto
    {
        public List<ShiftClosingReportItemDto> Items { get; set; } = new();
    }

    public class ShiftClosingReportItemDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public int SystemCount { get; set; }
        public int ActualCount { get; set; }
        public int Difference { get; set; }
    }
}