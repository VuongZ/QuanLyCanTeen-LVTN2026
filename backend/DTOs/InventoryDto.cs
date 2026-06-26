namespace LuanVanTotNghiep.DTOs
{
    public class InventoryDto
    {
        public int Id { get; set; }
        public string BranchName { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int? Quantity { get; set; }
        public string Unit { get; set; } = null!;
    }
}