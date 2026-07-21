using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }
    }

    public class CreateUpdateSupplierDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên nhà phân phối.")]
        [StringLength(
            150,
            ErrorMessage = "Tên nhà phân phối không được vượt quá 150 ký tự."
        )]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(
            20,
            ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự."
        )]
        public string? Phone { get; set; }

        [StringLength(
            255,
            ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự."
        )]
        public string? Address { get; set; }
    }
}