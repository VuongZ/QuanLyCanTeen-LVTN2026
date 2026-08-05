using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class StaffRegistrationService
{
    // Nhân viên PART_TIME đăng ký ca.
    //
    // Nếu còn vị trí chính thức:
    // REGISTERED.
    //
    // Nếu hết vị trí chính thức:
    // WAITLIST.
    public async Task<CaStaffRegistration> RegisterAsync(
        RegisterShiftDto dto)
    {
        // Dùng transaction SERIALIZABLE để hạn chế trường hợp
        // nhiều nhân viên đăng ký cùng một vị trí tại cùng thời điểm.
        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

        var period =
            await _repo.GetPeriodByIdAsync(
                dto.PeriodId);

        if (period == null)
        {
            throw new KeyNotFoundException(
                "Đợt đăng ký không tồn tại.");
        }

        if (!string.Equals(
                period.Status,
                "OPEN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã khóa hoặc đã công bố.");
        }

        var today =
            GetVietnamToday();

        // Khi đã đến ngày bắt đầu của đợt,
        // Staff không được đăng ký hoặc thay đổi ca nữa.
        if (today >= period.StartDate)
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã hết thời gian tiếp nhận đăng ký ca.");
        }

        if (dto.WorkDate < period.StartDate ||
            dto.WorkDate > period.EndDate)
        {
            throw new ArgumentException(
                "Ngày đăng ký không nằm trong thời gian của đợt này.");
        }

        var user =
            await _repo.GetUserByIdAsync(
                dto.UserId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        if (user.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Bạn chỉ được đăng ký ca làm tại chi nhánh của mình.");
        }

        // FULL_TIME và MATERNITY được tự động xếp lịch,
        // nên không đăng ký giống PART_TIME.
        if (SalaryWagePolicy.IsFullTimeEquivalent(
                user.EmploymentType))
        {
            throw new InvalidOperationException(
                "Nhân viên FULL_TIME hoặc Thai sản được hệ thống " +
                "tự động xếp vào mọi ca hoạt động trong tuần, " +
                "không cần đăng ký ca.");
        }

        var shift =
            await _repo.GetShiftByIdAsync(
                dto.ShiftId);

        if (shift == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy ca làm.");
        }

        if (shift.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Ca làm không thuộc chi nhánh của đợt đăng ký.");
        }

        var targetDay =
            dto.WorkDate.DayOfWeek.ToString();

        var config =
            await _repo.GetShiftConfigAsync(
                dto.ShiftId,
                targetDay);

        // MaxStaff bằng 0 nghĩa là ca không được mở
        // trong ngày Staff đang chọn.
        if (config == null ||
            config.MaxStaff.GetValueOrDefault() <= 0)
        {
            string[] vietnameseDays =
            {
                "Chủ nhật",
                "Thứ 2",
                "Thứ 3",
                "Thứ 4",
                "Thứ 5",
                "Thứ 6",
                "Thứ 7"
            };

            var vietnameseDayName =
                vietnameseDays[
                    (int)dto.WorkDate.DayOfWeek];

            throw new InvalidOperationException(
                $"Ca làm này không mở vào {vietnameseDayName}.");
        }

        // Không cho một nhân viên có hai phiếu chưa hủy
        // trong cùng đợt, cùng ngày và cùng ca.
        var isDuplicate =
            await _repo
                .HasNonCancelledRegistrationAsync(
                    dto.PeriodId,
                    dto.UserId,
                    dto.ShiftId,
                    dto.WorkDate);

        if (isDuplicate)
        {
            throw new InvalidOperationException(
                "Bạn đã đăng ký ca này vào ngày này rồi.");
        }

        // MaxStaff bao gồm:
        // - 1 vị trí Quản lý.
        // - Nhân viên FULL_TIME/MATERNITY tự động xếp.
        // - Nhân viên PART_TIME đăng ký.
        var maxStaff =
            config.MaxStaff.GetValueOrDefault();

        var fullTimeStaffCount =
            await _repo.CountBranchFullTimeStaffAsync(
                period.BranchId);

        // Số vị trí chính thức dành cho PART_TIME.
        //
        // Giá trị có thể bằng 0.
        // Khi bằng 0, PART_TIME vẫn được đăng ký
        // nhưng sẽ được đưa vào WAITLIST.
        var staffSlot =
            Math.Max(
                maxStaff -
                1 -
                fullTimeStaffCount,
                0);

        // Chỉ đếm REGISTERED.
        // WAITLIST không chiếm vị trí chính thức.
        // Kể cả khi Quản lý và FULL_TIME đã giữ hết chỗ
        // (staffSlot = 0), PART_TIME vẫn được đăng ký WAITLIST
        // để có thể thay thế khi nhân viên chính thức nghỉ.
        var registeredCount =
            await _repo.CountRegisteredAsync(
                dto.PeriodId,
                dto.ShiftId,
                dto.WorkDate);

        // Còn vị trí thì REGISTERED.
        // Hết vị trí hoặc staffSlot bằng 0 thì WAITLIST.
        var assignedStatus =
            registeredCount < staffSlot
                ? RegisteredStatus
                : WaitlistStatus;

        var registration =
            new CaStaffRegistration
            {
                UserId =
                    dto.UserId,

                PeriodId =
                    dto.PeriodId,

                ShiftId =
                    dto.ShiftId,

                WorkDate =
                    dto.WorkDate,

                Status =
                    assignedStatus,

                RegisteredAt =
                    GetVietnamNow()
            };

        await _repo.Add(registration);

        await transaction.CommitAsync();

        return registration;
    }
}