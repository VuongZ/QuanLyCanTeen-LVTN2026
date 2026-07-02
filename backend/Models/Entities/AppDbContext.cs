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

    public virtual DbSet<CaBranchShiftConfig> CaBranchShiftConfigs { get; set; }

    public virtual DbSet<CaFinalSchedule> CaFinalSchedules { get; set; }

    public virtual DbSet<CaSchedulePeriod> CaSchedulePeriods { get; set; }

    public virtual DbSet<CaShift> CaShifts { get; set; }

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

    public virtual DbSet<LuongSalaryRule> LuongSalaryRules { get; set; }

    public virtual DbSet<NsRole> NsRoles { get; set; }

    public virtual DbSet<NsUser> NsUsers { get; set; }

   // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //  => optionsBuilder.UseMySql("server=localhost;port=3306;database=qlcanteen;user=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.1.0-mysql"));

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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_final_schedule")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ShiftId, "fk_final_shift");

            entity.HasIndex(e => e.UserId, "fk_final_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'DRAFT'")
                .HasColumnType("enum('DRAFT','PUBLISHED')")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaFinalSchedules)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("fk_final_shift");

            entity.HasOne(d => d.User).WithMany(p => p.CaFinalSchedules)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_final_user");
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
                .HasColumnType("enum('OPEN','DRAFT','PUBLISHED')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ca_staff_registration")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.PeriodId, "fk_reg_period");

            entity.HasIndex(e => e.ShiftId, "fk_reg_shift");

            entity.HasIndex(e => e.UserId, "fk_reg_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PeriodId).HasColumnName("period_id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Chờ Duyệt'")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");

            entity.HasOne(d => d.Period).WithMany(p => p.CaStaffRegistrations)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_reg_period");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaStaffRegistrations)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("fk_reg_shift");

            entity.HasOne(d => d.User).WithMany(p => p.CaStaffRegistrations)
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

            entity
                .ToTable("kho_branch_front_stock")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_front_branch");

            entity.HasIndex(e => e.ProductId, "fk_front_product");

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
                .HasConstraintName("fk_front_product");
        });

        modelBuilder.Entity<KhoBranchInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_branch_inventory")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "branch_id");

            entity.HasIndex(e => e.ProductId, "product_id");

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

            entity
                .ToTable("kho_export_detail")
                .UseCollation("utf8mb4_unicode_ci");

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

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ExportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("export_date");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoExportTickets)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_exp_branch");

            entity.HasOne(d => d.Manager).WithMany(p => p.KhoExportTickets)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("fk_exp_manager");
        });

        modelBuilder.Entity<KhoImportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_import_detail")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ImportId, "fk_impdet_import");

            entity.HasIndex(e => e.ProductId, "fk_impdet_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ImportId).HasColumnName("import_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Import).WithMany(p => p.KhoImportDetails)
                .HasForeignKey(d => d.ImportId)
                .HasConstraintName("fk_impdet_import");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoImportDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_impdet_product");
        });

        modelBuilder.Entity<KhoImportTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_import_ticket")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_imp_branch");

            entity.HasIndex(e => e.ManagerId, "fk_imp_manager");

            entity.HasIndex(e => e.SupplierId, "fk_imp_supplier");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ImportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("import_date");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

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

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");

            entity.HasOne(d => d.Supplier).WithMany(p => p.KhoProducts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_supplier");
        });

        modelBuilder.Entity<KhoShiftClosingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("kho_shift_closing_detail")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductId, "fk_detail_product");

            entity.HasIndex(e => e.ReportId, "fk_detail_report");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualCount).HasColumnName("actual_count");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ReportId).HasColumnName("report_id");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoShiftClosingDetails)
                .HasForeignKey(d => d.ProductId)
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

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ReportDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("report_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoShiftClosingReports)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("fk_report_branch");

            entity.HasOne(d => d.User).WithMany(p => p.KhoShiftClosingReports)
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

            entity.HasOne(d => d.Branch).WithMany(p => p.LuongSalaryRules)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("luong_salary_rule_ibfk_1");
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
            entity.Property(e => e.SeniorWage)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("senior_wage");
        });

        modelBuilder.Entity<NsUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("ns_user")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BranchId, "fk_user_branch");

            entity.HasIndex(e => e.RoleId, "fk_user_role");

            entity.HasIndex(e => e.Username, "username").IsUnique();

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
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.HireDate)
                .HasDefaultValueSql("curdate()")
                .HasColumnName("hire_date");
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
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Branch).WithMany(p => p.NsUsers)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_branch");

            entity.HasOne(d => d.Role).WithMany(p => p.NsUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_user_role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
