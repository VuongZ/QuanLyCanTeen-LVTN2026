

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class CaShift
{
    public int Id { get; set; }

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int? BranchId { get; set; }

    public int? MaxStaff { get; set; }

    public bool? IsOt { get; set; }

    public DateTime? RowVersion { get; set; }

    public virtual DmBranch? Branch { get; set; }

    public virtual ICollection<CaBranchShiftConfig> CaBranchShiftConfigs { get; set; } = new List<CaBranchShiftConfig>();

    public virtual ICollection<CaFinalSchedule> CaFinalSchedules { get; set; } = new List<CaFinalSchedule>();

    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();
}
