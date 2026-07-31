using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CaAttendance> CaAttendances { get; set; }

    public virtual DbSet<CaCheckoutRequest> CaCheckoutRequests { get; set; }

    public virtual DbSet<CaCheckoutRequestHistory> CaCheckoutRequestHistories { get; set; }

    public virtual DbSet<CaBranchShiftConfig> CaBranchShiftConfigs { get; set; }

    public virtual DbSet<CaFinalSchedule> CaFinalSchedules { get; set; }

    public virtual DbSet<CaSchedulePeriod> CaSchedulePeriods { get; set; }

    public virtual DbSet<CaShift> CaShifts { get; set; }

    public virtual DbSet<CaShiftDelegation> CaShiftDelegations { get; set; }

    public virtual DbSet<CaShiftDelegationAudit> CaShiftDelegationAudits { get; set; }

    public virtual DbSet<CaStaffRegistration> CaStaffRegistrations { get; set; }

    public virtual DbSet<DmBranch> DmBranches { get; set; }

    public virtual DbSet<KhoBranchFrontStock> KhoBranchFrontStocks { get; set; }

    public virtual DbSet<KhoBranchInventory> KhoBranchInventories { get; set; }

    public virtual DbSet<KhoExportDetail> KhoExportDetails { get; set; }

    public virtual DbSet<KhoExportTicket> KhoExportTickets { get; set; }

    public virtual DbSet<KhoImportDetail> KhoImportDetails { get; set; }

    public virtual DbSet<KhoImportTicket> KhoImportTickets { get; set; }

    public virtual DbSet<KhoProduct> KhoProducts { get; set; }

    public virtual DbSet<KhoShiftClosingDetail> KhoShiftClosingDetails { get; set; }

    public virtual DbSet<KhoShiftClosingReport> KhoShiftClosingReports { get; set; }

    public virtual DbSet<KhoSupplier> KhoSuppliers { get; set; }

    public virtual DbSet<LuongMonthlySalary> LuongMonthlySalaries { get; set; }

    public virtual DbSet<LuongSalaryAdjustmentHistory> LuongSalaryAdjustmentHistories { get; set; }

    public virtual DbSet<LuongSalaryComplaint> LuongSalaryComplaints { get; set; }

    public virtual DbSet<LuongSalaryRule> LuongSalaryRules { get; set; }

    public virtual DbSet<LuongSalaryTransfer> LuongSalaryTransfers { get; set; }

    public virtual DbSet<NsRole> NsRoles { get; set; }

    public virtual DbSet<NsUser> NsUsers { get; set; }

    public virtual DbSet<NsUserBankAccount> NsUserBankAccounts { get; set; }

    // Không cấu hình connection string trực tiếp tại đây.
    // AppDbContext được cấu hình bằng Dependency Injection
    // trong Program.cs.
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<CaAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_attendance")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SalaryId, "fk_att_salary");

            entity.HasIndex(e => e.ScheduleId, "fk_att_schedule");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CheckInTime)
                .HasColumnType("datetime")
                .HasColumnName("check_in_time");
            entity.Property(e => e.CheckOutTime)
                .HasColumnType("datetime")
                .HasColumnName("check_out_time");
            entity.Property(e => e.SalaryId).HasColumnName("salary_id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Chưa chấm công'")
                .HasColumnName("status");

            entity.HasOne(d => d.Salary).WithMany(p => p.CaAttendances)
                .HasForeignKey(d => d.SalaryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_att_salary");

            entity.HasOne(d => d.Schedule).WithMany(p => p.CaAttendances)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("fk_att_schedule");
        });

        modelBuilder.Entity<CaCheckoutRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("ca_checkout_request").UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.AttendanceId, "idx_checkout_request_attendance").IsUnique();
            entity.HasIndex(e => new { e.Status, e.UpdatedAt }, "idx_checkout_request_status");
            entity.HasIndex(e => e.RequestedByUserId, "idx_checkout_request_user");
            entity.HasIndex(e => e.ReviewedByUserId, "idx_checkout_request_reviewer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
            entity.Property(e => e.ProposedCheckOutTime).HasColumnType("datetime").HasColumnName("proposed_check_out_time");
            entity.Property(e => e.RequestedCheckOutTime).HasColumnType("datetime").HasColumnName("requested_check_out_time");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            entity.Property(e => e.Status).HasMaxLength(30).HasColumnName("status");
            entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            entity.Property(e => e.RejectReason).HasMaxLength(500).HasColumnName("reject_reason");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasColumnName("updated_at");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime").HasColumnName("reviewed_at");

            entity.HasOne(e => e.Attendance).WithMany(a => a.CheckoutRequests)
                .HasForeignKey(e => e.AttendanceId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_checkout_request_attendance");
            entity.HasOne(e => e.RequestedByUser).WithMany()
                .HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_checkout_request_user");
            entity.HasOne(e => e.ReviewedByUser).WithMany()
                .HasForeignKey(e => e.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_checkout_request_reviewer");
        });

        modelBuilder.Entity<CaCheckoutRequestHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("ca_checkout_request_history").UseCollation("utf8mb4_unicode_ci");
            entity.HasIndex(e => e.RequestId, "idx_checkout_history_request");
            entity.HasIndex(e => e.ActorUserId, "idx_checkout_history_actor");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.Action).HasMaxLength(30).HasColumnName("action");
            entity.Property(e => e.Detail).HasMaxLength(1000).HasColumnName("detail");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasColumnName("created_at");
            entity.HasOne(e => e.Request).WithMany(r => r.History).HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_checkout_history_request");
            entity.HasOne(e => e.ActorUser).WithMany().HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull).HasConstraintName("fk_checkout_history_actor");
        });

        modelBuilder.Entity<CaBranchShiftConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_branch_shift_config")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ShiftId, "fk_config_shift");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DayOfWeek)
                .HasColumnType("enum('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday')")
                .HasColumnName("day_of_week");
            entity.Property(e => e.MaxStaff)
                .HasDefaultValueSql("'3'")
                .HasColumnName("max_staff");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("row_version");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaBranchShiftConfigs)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("fk_config_shift");
        });

        modelBuilder.Entity<CaFinalSchedule>(entity =>
{
    entity.HasKey(e => e.Id)
        .HasName("PRIMARY");

    entity
        .ToTable("ca_final_schedule")
        .UseCollation("utf8mb4_unicode_ci");

    entity.HasIndex(
            e => new
            {
                e.UserId,
                e.ShiftId,
                e.WorkDate
            },
            "uq_final_user_shift_date")
        .IsUnique();

    entity.HasIndex(
            e => e.SourceRegistrationId,
            "uq_final_source_registration")
        .IsUnique();

    entity.HasIndex(
            e => e.ReplacesScheduleId,
            "uq_final_replaces_schedule")
        .IsUnique();

    entity.HasIndex(
        e => e.PeriodId,
        "idx_final_period");

    entity.HasIndex(
        e => new
        {
            e.AssignmentType,
            e.Status
        },
        "idx_final_assignment");

    entity.HasIndex(
        e => e.AbsenceMarkedByUserId,
        "idx_final_absence_manager");

    entity.HasIndex(
        e => e.AssignedByUserId,
        "idx_final_assigned_manager");

    entity.HasIndex(
        e => e.ShiftId,
        "fk_final_shift");

    entity.HasIndex(
        e => e.UserId,
        "fk_final_user");

    entity.Property(e => e.Id)
        .HasColumnName("id");

    entity.Property(e => e.PeriodId)
        .HasColumnName("period_id");

    entity.Property(e => e.SourceRegistrationId)
        .HasColumnName("source_registration_id");

    entity.Property(e => e.UserId)
        .HasColumnName("user_id");

    entity.Property(e => e.ShiftId)
        .HasColumnName("shift_id");

    entity.Property(e => e.WorkDate)
        .HasColumnName("work_date");

    entity.Property(e => e.Status)
        .HasDefaultValueSql("'DRAFT'")
        .HasColumnType(
            "enum(" +
            "'DRAFT'," +
            "'PUBLISHED'," +
            "'LEAVE_APPROVED'," +
            "'ABSENT'," +
            "'CANCELLED'" +
            ")")
        .HasColumnName("status");

    entity.Property(e => e.AssignmentType)
        .HasDefaultValueSql("'NORMAL'")
        .HasColumnType(
            "enum(" +
            "'NORMAL'," +
            "'EMERGENCY_REPLACEMENT'" +
            ")")
        .HasColumnName("assignment_type");

    entity.Property(e => e.PayMultiplier)
        .HasPrecision(5, 2)
        .HasDefaultValueSql("'1.00'")
        .HasColumnName("pay_multiplier");

    entity.Property(e => e.ReplacesScheduleId)
        .HasColumnName("replaces_schedule_id");

    entity.Property(e => e.AbsenceReason)
        .HasMaxLength(500)
        .HasColumnName("absence_reason");

    entity.Property(e => e.AbsenceMarkedByUserId)
        .HasColumnName(
            "absence_marked_by_user_id");

    entity.Property(e => e.AbsenceMarkedAt)
        .HasColumnType("datetime")
        .HasColumnName("absence_marked_at");

    entity.Property(e => e.AssignedByUserId)
        .HasColumnName("assigned_by_user_id");

    entity.Property(e => e.AssignedAt)
        .HasColumnType("datetime")
        .HasColumnName("assigned_at");

    entity.HasOne(d => d.Shift)
        .WithMany(p => p.CaFinalSchedules)
        .HasForeignKey(d => d.ShiftId)
        .HasConstraintName("fk_final_shift");

    entity.HasOne(d => d.User)
        .WithMany(p => p.CaFinalSchedules)
        .HasForeignKey(d => d.UserId)
        .HasConstraintName("fk_final_user");

    entity.HasOne(d => d.Period)
        .WithMany()
        .HasForeignKey(d => d.PeriodId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName("fk_final_period");

    entity.HasOne(d => d.SourceRegistration)
        .WithOne()
        .HasForeignKey<CaFinalSchedule>(
            d => d.SourceRegistrationId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName(
            "fk_final_source_registration");

    entity.HasOne(d => d.ReplacesSchedule)
        .WithOne(p => p.ReplacementSchedule)
        .HasForeignKey<CaFinalSchedule>(
            d => d.ReplacesScheduleId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName(
            "fk_final_replaces_schedule");

    entity.HasOne(d => d.AbsenceMarkedByUser)
        .WithMany()
        .HasForeignKey(
            d => d.AbsenceMarkedByUserId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName(
            "fk_final_absence_manager");

    entity.HasOne(d => d.AssignedByUser)
        .WithMany()
        .HasForeignKey(
            d => d.AssignedByUserId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName(
            "fk_final_assigned_manager");
});

        modelBuilder.Entity<CaShiftDelegation>(entity =>
        {
            entity.ToTable("ca_shift_delegation").UseCollation("utf8mb4_unicode_ci");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.HasIndex(e => new { e.BranchId, e.ShiftId, e.WorkDate }, "idx_delegation_shift_date");
            entity.HasIndex(e => e.DelegateUserId, "idx_delegation_delegate");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");
            entity.Property(e => e.DelegatedByUserId).HasColumnName("delegated_by_user_id");
            entity.Property(e => e.DelegateUserId).HasColumnName("delegate_user_id");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("PENDING").HasColumnName("status");
            entity.Property(e => e.StartsAtUtc).HasColumnType("datetime").HasColumnName("starts_at_utc");
            entity.Property(e => e.EndsAtUtc).HasColumnType("datetime").HasColumnName("ends_at_utc");
            entity.Property(e => e.RequestedAtUtc).HasColumnType("datetime").HasColumnName("requested_at_utc");
            entity.Property(e => e.RespondedAtUtc).HasColumnType("datetime").HasColumnName("responded_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnType("datetime").HasColumnName("revoked_at_utc");
            entity.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Shift).WithMany().HasForeignKey(e => e.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DelegatedByUser).WithMany().HasForeignKey(e => e.DelegatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DelegateUser).WithMany().HasForeignKey(e => e.DelegateUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaShiftDelegationAudit>(entity =>
        {
            entity.ToTable("ca_shift_delegation_audit").UseCollation("utf8mb4_unicode_ci");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.HasIndex(e => e.DelegationId, "idx_delegation_audit");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DelegationId).HasColumnName("delegation_id");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.ActionType).HasMaxLength(50).HasColumnName("action_type");
            entity.Property(e => e.Details).HasMaxLength(1000).HasColumnName("details");
            entity.Property(e => e.OccurredAtUtc).HasColumnType("datetime").HasColumnName("occurred_at_utc");
            entity.HasOne(e => e.Delegation).WithMany(d => d.Audits).HasForeignKey(e => e.DelegationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActorUser).WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaSchedulePeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_schedule_period")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'OPEN'")
                .HasColumnType("enum('OPEN','DRAFT','PUBLISHED','REVIEWING','CLOSED')")
                .HasColumnName("status");
        });

        modelBuilder.Entity<CaShift>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_shift")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_branch_shift");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.IsOt)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_ot");
            entity.Property(e => e.MaxStaff)
                .HasDefaultValueSql("'3'")
                .HasColumnName("max_staff");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("row_version");
            entity.Property(e => e.ShiftName)
                .HasMaxLength(50)
                .HasColumnName("shift_name");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");

            entity.HasOne(d => d.Branch).WithMany(p => p.CaShifts)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_branch_shift");
        });

       modelBuilder.Entity<CaStaffRegistration>(entity =>
{
    entity.HasKey(e => e.Id)
        .HasName("PRIMARY");

    entity
        .ToTable("ca_staff_registration")
        .UseCollation("utf8mb4_unicode_ci");

    entity.HasIndex(
        e => e.ShiftId,
        "fk_reg_shift");

    entity.HasIndex(
        e => e.UserId,
        "fk_reg_user");

    entity.HasIndex(
        e => new
        {
            e.PeriodId,
            e.ShiftId,
            e.WorkDate,
            e.Status,
            e.RegisteredAt
        },
        "idx_registration_waitlist");

    entity.Property(e => e.Id)
        .HasColumnName("id");

    entity.Property(e => e.UserId)
        .HasColumnName("user_id");

    entity.Property(e => e.ShiftId)
        .HasColumnName("shift_id");

    entity.Property(e => e.WorkDate)
        .HasColumnName("work_date");

    entity.Property(e => e.Status)
        .HasColumnType(
            "enum(" +
            "'REGISTERED'," +
            "'WAITLIST'," +
            "'CANCELLED'," +
            "'REPLACEMENT_SELECTED'" +
            ")")
        .HasColumnName("status");

    entity.Property(e => e.PeriodId)
        .HasColumnName("period_id");

    entity.Property(e => e.RegisteredAt)
        .HasDefaultValueSql(
            "CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("registered_at");

    entity.HasOne(d => d.Period)
        .WithMany(p =>
            p.CaStaffRegistrations)
        .HasForeignKey(d => d.PeriodId)
        .OnDelete(DeleteBehavior.Cascade)
        .HasConstraintName("fk_reg_period");

    entity.HasOne(d => d.Shift)
        .WithMany(p =>
            p.CaStaffRegistrations)
        .HasForeignKey(d => d.ShiftId)
        .HasConstraintName("fk_reg_shift");

    entity.HasOne(d => d.User)
        .WithMany(p =>
            p.CaStaffRegistrations)
        .HasForeignKey(d => d.UserId)
        .HasConstraintName("fk_reg_user");
});

        modelBuilder.Entity<DmBranch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("dm_branch")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Latitude)
                .HasPrecision(10, 8)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8)
                .HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<KhoBranchFrontStock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_branch_front_stock");

            entity.HasIndex(e => e.BranchId, "fk_front_branch");

            entity.HasIndex(e => e.ProductId, "fk_front_product");

            entity.HasIndex(e => new { e.BranchId, e.ProductId }, "uq_front_stock_branch_product").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'0'")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoBranchFrontStocks)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_front_branch");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoBranchFrontStocks)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_front_product");
        });

        modelBuilder.Entity<KhoBranchInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_branch_inventory");

            entity.HasIndex(e => e.BranchId, "branch_id");

            entity.HasIndex(e => e.ProductId, "product_id");

            entity.HasIndex(e => new { e.BranchId, e.ProductId }, "uq_inventory_branch_product").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'0'")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoBranchInventories)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("kho_branch_inventory_ibfk_1");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoBranchInventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("kho_branch_inventory_ibfk_2");
        });

        modelBuilder.Entity<KhoExportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_export_detail");

            entity.HasIndex(e => e.ExportId, "fk_det_export");

            entity.HasIndex(e => e.ProductId, "fk_expdet_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExportId).HasColumnName("export_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Export).WithMany(p => p.KhoExportDetails)
                .HasForeignKey(d => d.ExportId)
                .HasConstraintName("fk_det_export");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoExportDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_expdet_product");
        });

        modelBuilder.Entity<KhoExportTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_export_ticket")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_exp_branch");

            entity.HasIndex(e => e.ManagerId, "fk_exp_manager");

            entity.HasIndex(e => e.ScheduleId, "idx_export_schedule");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ExportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("export_date");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoExportTickets)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_exp_branch");

            entity.HasOne(d => d.Manager).WithMany(p => p.KhoExportTickets)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("fk_exp_manager");

            entity.HasOne(d => d.Schedule).WithMany(p => p.KhoExportTickets)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_export_schedule");
        });

        modelBuilder.Entity<KhoImportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_import_detail");

            entity.HasIndex(e => e.ImportId, "fk_impdet_import");

            entity.HasIndex(e => e.ProductId, "fk_impdet_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ImportId).HasColumnName("import_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitAtTime)
                .HasMaxLength(50)
                .HasColumnName("unit_at_time")
                .UseCollation("utf8mb4_unicode_ci");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Import).WithMany(p => p.KhoImportDetails)
                .HasForeignKey(d => d.ImportId)
                .HasConstraintName("fk_impdet_import");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoImportDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_impdet_product");
        });

        modelBuilder.Entity<KhoImportTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_import_ticket");

            entity.HasIndex(e => e.BranchId, "fk_imp_branch");

            entity.HasIndex(e => e.ManagerId, "fk_imp_manager");

            entity.HasIndex(e => e.SupplierId, "fk_imp_supplier");

            entity.HasIndex(e => new { e.SupplierId, e.InvoiceCode }, "uq_import_supplier_invoice").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ImportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("import_date");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(50)
                .HasColumnName("invoice_code")
                .UseCollation("utf8mb4_unicode_ci");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note")
                .UseCollation("utf8mb4_unicode_ci");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(15, 2)
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoImportTickets)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_imp_branch");

            entity.HasOne(d => d.Manager).WithMany(p => p.KhoImportTickets)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("fk_imp_manager");

            entity.HasOne(d => d.Supplier).WithMany(p => p.KhoImportTickets)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("fk_imp_supplier");
        });

        modelBuilder.Entity<KhoProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_product")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SupplierId, "fk_product_supplier");

            entity.HasIndex(e => e.InactiveBy, "idx_product_inactive_by");

            entity.HasIndex(e => e.IsActive, "idx_product_is_active");

            entity.HasIndex(e => e.ProductName, "idx_product_name");

            entity.HasIndex(e => e.ProductCode, "uq_product_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InactiveAt)
                .HasColumnType("datetime")
                .HasColumnName("inactive_at");
            entity.Property(e => e.InactiveBy).HasColumnName("inactive_by");
            entity.Property(e => e.InactiveReason)
                .HasMaxLength(255)
                .HasColumnName("inactive_reason");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(50)
                .HasColumnName("product_code");
            entity.Property(e => e.ProductName).HasColumnName("product_name");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");

            entity.HasOne(d => d.InactiveByNavigation).WithMany(p => p.KhoProducts)
                .HasForeignKey(d => d.InactiveBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_inactive_by");

            entity.HasOne(d => d.Supplier).WithMany(p => p.KhoProducts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_supplier");
        });

        modelBuilder.Entity<KhoShiftClosingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("kho_shift_closing_detail");

            entity.HasIndex(e => e.ProductId, "fk_detail_product");

            entity.HasIndex(e => e.ReportId, "fk_detail_report");

            entity.HasIndex(e => new { e.ReportId, e.ProductId }, "uq_closing_detail_report_product").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualCount).HasColumnName("actual_count");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.SystemCount).HasColumnName("system_count");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoShiftClosingDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detail_product");

            entity.HasOne(d => d.Report).WithMany(p => p.KhoShiftClosingDetails)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("fk_detail_report");
        });

        modelBuilder.Entity<KhoShiftClosingReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_shift_closing_report")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_report_branch");

            entity.HasIndex(e => e.UserId, "fk_report_user");

            entity.HasIndex(e => e.ReviewedBy, "idx_closing_report_reviewed_by");

            entity.HasIndex(e => e.Status, "idx_closing_report_status");

            entity.HasIndex(e => e.ScheduleId, "idx_report_schedule").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(500)
                .HasColumnName("reject_reason");
            entity.Property(e => e.ReportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("report_date");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("datetime")
                .HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoShiftClosingReports)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_report_branch");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.KhoShiftClosingReportReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_closing_report_reviewed_by");

            entity.HasOne(d => d.Schedule).WithOne(p => p.KhoShiftClosingReport)
                .HasForeignKey<KhoShiftClosingReport>(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_report_schedule");

            entity.HasOne(d => d.User).WithMany(p => p.KhoShiftClosingReportUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_report_user");
        });

        modelBuilder.Entity<KhoSupplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_supplier")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp")
                .HasColumnName("deleted_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_deleted");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(255)
                .HasColumnName("supplier_name");
        });

        modelBuilder.Entity<LuongMonthlySalary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("luong_monthly_salary")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.UserId, e.Month, e.Year }, "unique_user_month_year").IsUnique();

            entity.HasIndex(e => e.FinalizedByUserId, "idx_monthly_salary_finalized_by");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.HourlyWageAtTime)
                .HasPrecision(10, 2)
                .HasColumnName("hourly_wage_at_time");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.FinalizedAt)
                .HasColumnType("datetime")
                .HasColumnName("finalized_at");
            entity.Property(e => e.FinalizedByUserId).HasColumnName("finalized_by_user_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnName("status");
            entity.Property(e => e.TotalBonus)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total_bonus");
            entity.Property(e => e.TotalHours)
                .HasPrecision(10, 2)
                .HasColumnName("total_hours");
            entity.Property(e => e.TotalPenalty)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total_penalty");
            entity.Property(e => e.TotalSalary)
                .HasPrecision(15, 2)
                .HasColumnName("total_salary");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Year).HasColumnName("year");

            entity.HasOne(d => d.User).WithMany(p => p.LuongMonthlySalaries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("luong_monthly_salary_ibfk_1");

            entity.HasOne(d => d.FinalizedByUser).WithMany(p => p.FinalizedMonthlySalaries)
                .HasForeignKey(d => d.FinalizedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_monthly_salary_finalized_by");

        });

        modelBuilder.Entity<LuongSalaryAdjustmentHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("luong_salary_adjustment_history").UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SalaryId, "idx_salary_adjustment_salary");
            entity.HasIndex(e => new { e.UserId, e.Year, e.Month }, "idx_salary_adjustment_user_period");
            entity.HasIndex(e => e.CreatedByUserId, "idx_salary_adjustment_creator");
            entity.HasIndex(e => e.ReviewedByUserId, "idx_salary_adjustment_reviewer");
            entity.HasIndex(e => e.Status, "idx_salary_adjustment_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SalaryId).HasColumnName("salary_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.BonusAmount).HasPrecision(15, 2).HasColumnName("bonus_amount");
            entity.Property(e => e.PenaltyAmount).HasPrecision(15, 2).HasColumnName("penalty_amount");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnName("status");
            entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime").HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewNote).HasMaxLength(500).HasColumnName("review_note");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasColumnName("created_at");

            entity.HasOne(e => e.Salary).WithMany(s => s.AdjustmentHistories)
                .HasForeignKey(e => e.SalaryId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_salary_adjustment_salary");
            entity.HasOne(e => e.User).WithMany(u => u.SalaryAdjustmentHistories)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_adjustment_user");
            entity.HasOne(e => e.CreatedByUser).WithMany(u => u.CreatedSalaryAdjustmentHistories)
                .HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_adjustment_creator");
            entity.HasOne(e => e.ReviewedByUser).WithMany(u => u.ReviewedSalaryAdjustmentHistories)
                .HasForeignKey(e => e.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_salary_adjustment_reviewer");
        });

        modelBuilder.Entity<LuongSalaryComplaint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("luong_salary_complaint").UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SalaryId, "uq_salary_complaint_salary").IsUnique();
            entity.HasIndex(e => e.UserId, "idx_salary_complaint_user");
            entity.HasIndex(e => e.ReviewedByUserId, "idx_salary_complaint_reviewer");
            entity.HasIndex(e => e.Status, "idx_salary_complaint_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SalaryId).HasColumnName("salary_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Content).HasMaxLength(1000).HasColumnName("content");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnName("status");
            entity.Property(e => e.ManagerResponse).HasMaxLength(1000).HasColumnName("manager_response");
            entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime").HasColumnName("reviewed_at");

            entity.HasOne(e => e.Salary).WithMany(s => s.Complaints)
                .HasForeignKey(e => e.SalaryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_salary_complaint_salary");
            entity.HasOne(e => e.User).WithMany(u => u.SalaryComplaints)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_complaint_user");
            entity.HasOne(e => e.ReviewedByUser).WithMany(u => u.ReviewedSalaryComplaints)
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_salary_complaint_reviewer");
        });

        modelBuilder.Entity<LuongSalaryRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("luong_salary_rule")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "branch_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AbsentPenalty)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'50000.00'")
                .HasColumnName("absent_penalty");
            entity.Property(e => e.BonusAmount)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'300000.00'")
                .HasColumnName("bonus_amount");
            entity.Property(e => e.BonusThresholdDays)
                .HasDefaultValueSql("'15'")
                .HasColumnName("bonus_threshold_days");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.LatePenalty)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'20000.00'")
                .HasColumnName("late_penalty");
            entity.Property(e => e.WeekendMultiplier)
                .HasDefaultValueSql("'1.5'")
                .HasColumnName("weekend_multiplier");
                entity.Property(
        e => e.EmergencyReplacementMultiplier)
    .HasPrecision(5, 2)
    .HasDefaultValueSql("'1.50'")
    .HasColumnName(
        "emergency_replacement_multiplier");

            entity.HasOne(d => d.Branch).WithMany(p => p.LuongSalaryRules)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("luong_salary_rule_ibfk_1");
        });

        modelBuilder.Entity<LuongSalaryTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("luong_salary_transfer");

            entity.HasIndex(e => new { e.BranchId, e.Month, e.Year }, "uq_salary_transfer_branch_period").IsUnique();
            entity.HasIndex(e => e.ManagerId, "idx_salary_transfer_manager");
            entity.HasIndex(e => e.TransferredByUserId, "idx_salary_transfer_admin");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.TransferredByUserId).HasColumnName("transferred_by_user_id");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.SalaryCount).HasColumnName("salary_count");
            entity.Property(e => e.TotalAmount).HasPrecision(15, 2).HasColumnName("total_amount");
            entity.Property(e => e.TransferredAt).HasColumnType("datetime").HasColumnName("transferred_at");

            entity.HasOne(d => d.Branch).WithMany()
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_transfer_branch");

            entity.HasOne(d => d.Manager).WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_transfer_manager");

            entity.HasOne(d => d.TransferredByUser).WithMany()
                .HasForeignKey(d => d.TransferredByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_salary_transfer_admin");
        });

        modelBuilder.Entity<NsRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ns_role")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.RoleName, "role_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.HourlyWage)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("hourly_wage");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<NsUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ns_user")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_user_branch");

            entity.HasIndex(e => e.RoleId, "fk_user_role");

            entity.HasIndex(e => e.Email, "uq_user_email").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "uq_user_phone").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.HireDate)
                .HasDefaultValueSql("curdate()")
                .HasColumnName("hire_date");
            entity.Property(e => e.EmploymentType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PART_TIME'")
                .HasColumnName("employment_type");
            entity.Property(e => e.SalaryCoefficient)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'1.00'")
                .HasColumnName("salary_coefficient");
            entity.Property(e => e.SalaryCoefficientIsManual)
                .HasDefaultValueSql("'0'")
                .HasColumnName("salary_coefficient_is_manual");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_deleted");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
            entity.Property(e => e.ResetPasswordCode)
                .HasMaxLength(10)
                .HasColumnName("reset_password_code");
            entity.Property(e => e.ResetPasswordExpiry)
                .HasColumnType("datetime")
                .HasColumnName("reset_password_expiry");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.NsUsers)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_branch");

            entity.HasOne(d => d.Role).WithMany(p => p.NsUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<NsUserBankAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ns_user_bank_account")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.UserId, "fk_bank_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BankAccountName)
                .HasMaxLength(100)
                .HasColumnName("bank_account_name");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(50)
                .HasColumnName("bank_account_number");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bank_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.NsUserBankAccounts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_bank_account_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
