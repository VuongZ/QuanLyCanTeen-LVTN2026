namespace LuanVanTotNghiep.DTOs
{
    public class ParsedInvoiceDto
    {
        public string? DetectedSupplierName { get; set; }
        public string? InvoiceCode { get; set; }
        public string? InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public float Confidence { get; set; }
        public string RawText { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
        public List<ParsedInvoiceItemDto> Items { get; set; } = new();
    }

    public class ParsedInvoiceItemDto
    {
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}