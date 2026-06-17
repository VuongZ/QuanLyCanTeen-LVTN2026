using System.Collections.Generic;

namespace LuanVanTotNghiep.DTOs;

public class PublishScheduleDto
{
    public int PeriodId { get; set; } // Đợt đăng ký nào đang được duyệt?
    public List<int> ApprovedRegistrationIds { get; set; } = new List<int>(); // Danh sách ID những người được chọn
}