namespace LuanVanTotNghiep.DTOs
{
    public class ExportScheduleOptionDto
    {
        public int ScheduleId { get; set; }

        public int ShiftId { get; set; }

        public string ShiftName { get; set; } = string.Empty;

        public string WorkDate { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public bool CanExportNow { get; set; }

        public string StatusLabel { get; set; } = string.Empty;
    }
}