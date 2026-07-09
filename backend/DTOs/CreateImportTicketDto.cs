namespace LuanVanTotNghiep.DTOs
{
    public class CreateImportTicketDto
    {
        public int ManagerId { get; set; }

        public int BranchId { get; set; }

        public int SupplierId { get; set; }

        public string? InvoiceCode { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public string? Note { get; set; }

        public List<ImportItemDto> Items { get; set; } = new();
    }
}