namespace LuanVanTotNghiep.DTOs
{
    public class CreateExportTicketDto
    {
        public int ManagerId { get; set; }

        public int BranchId { get; set; }
        public int? ScheduleId { get; set; }

        public string? Note { get; set; }

        public List<ExportItemDto> Items { get; set; } = new();
        
    }

    public class ExportItemDto
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}