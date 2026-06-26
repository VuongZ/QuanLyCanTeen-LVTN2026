namespace LuanVanTotNghiep.DTOs
{
    // DTO dùng để trả dữ liệu cho React hiển thị (có ID)
    public class SupplierDto
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    // DTO dùng để nhận dữ liệu từ React khi Thêm/Sửa (không cần ID)
    public class CreateUpdateSupplierDto
    {
        public string SupplierName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}