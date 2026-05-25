using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace LuanVanTotNghiep.Models.Entities;

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=qlcanteen;user=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.1.0-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<CaAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Status).HasDefaultValueSql("'Chưa chấm công'");

            entity.HasOne(d => d.Schedule).WithMany(p => p.CaAttendances).HasConstraintName("fk_att_schedule");
        });

        modelBuilder.Entity<CaBranchShiftConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.MaxStaff).HasDefaultValueSql("'3'");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Branch).WithMany(p => p.CaBranchShiftConfigs).HasConstraintName("fk_config_branch");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaBranchShiftConfigs).HasConstraintName("fk_config_shift");
        });

        modelBuilder.Entity<CaFinalSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Status).HasDefaultValueSql("'DRAFT'");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaFinalSchedules).HasConstraintName("fk_final_shift");

            entity.HasOne(d => d.User).WithMany(p => p.CaFinalSchedules).HasConstraintName("fk_final_user");
        });

        modelBuilder.Entity<CaSchedulePeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'OPEN'");
        });

        modelBuilder.Entity<CaShift>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.IsOt).HasDefaultValueSql("'0'");
            entity.Property(e => e.MaxStaff).HasDefaultValueSql("'3'");
            entity.Property(e => e.RowVersion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Branch).WithMany(p => p.CaShifts).HasConstraintName("fk_branch_shift");
        });

        modelBuilder.Entity<CaStaffRegistration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Status).HasDefaultValueSql("'Chờ Duyệt'");

            entity.HasOne(d => d.Shift).WithMany(p => p.CaStaffRegistrations).HasConstraintName("fk_reg_shift");

            entity.HasOne(d => d.User).WithMany(p => p.CaStaffRegistrations).HasConstraintName("fk_reg_user");
        });

        modelBuilder.Entity<DmBranch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<KhoBranchFrontStock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Quantity).HasDefaultValueSql("'0'");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoBranchFrontStocks).HasConstraintName("fk_front_branch");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoBranchFrontStocks).HasConstraintName("fk_front_product");
        });

        modelBuilder.Entity<KhoBranchInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Quantity).HasDefaultValueSql("'0'");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoBranchInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("kho_branch_inventory_ibfk_1");

            entity.HasOne(d => d.Product).WithMany(p => p.KhoBranchInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("kho_branch_inventory_ibfk_2");
        });

        modelBuilder.Entity<KhoExportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.Export).WithMany(p => p.KhoExportDetails).HasConstraintName("fk_det_export");

            entity.HasOne(d => d.FrontStock).WithMany(p => p.KhoExportDetails).HasConstraintName("fk_expdet_frontstock");

            entity.HasOne(d => d.Inventory).WithMany(p => p.KhoExportDetails).HasConstraintName("fk_expdet_inventory");
        });

        modelBuilder.Entity<KhoExportTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.ExportDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoExportTickets).HasConstraintName("fk_exp_branch");

            entity.HasOne(d => d.Manager).WithMany(p => p.KhoExportTickets).HasConstraintName("fk_exp_manager");
        });

        modelBuilder.Entity<KhoImportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.UnitPrice).HasDefaultValueSql("'0.00'");

            entity.HasOne(d => d.Import).WithMany(p => p.KhoImportDetails).HasConstraintName("fk_impdet_import");

            entity.HasOne(d => d.Inventory).WithMany(p => p.KhoImportDetails).HasConstraintName("fk_impdet_inventory");
        });

        modelBuilder.Entity<KhoImportTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.ImportDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoImportTickets).HasConstraintName("fk_imp_branch");

            entity.HasOne(d => d.Manager).WithMany(p => p.KhoImportTickets).HasConstraintName("fk_imp_manager");

            entity.HasOne(d => d.Supplier).WithMany(p => p.KhoImportTickets).HasConstraintName("fk_imp_supplier");
        });

        modelBuilder.Entity<KhoProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.Supplier).WithMany(p => p.KhoProducts)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_supplier");
        });

        modelBuilder.Entity<KhoShiftClosingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.ReportDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.FrontStock).WithMany(p => p.KhoShiftClosingDetails).HasConstraintName("fk_detail_frontstock");

            entity.HasOne(d => d.Report).WithMany(p => p.KhoShiftClosingDetails).HasConstraintName("fk_detail_report");
        });

        modelBuilder.Entity<KhoShiftClosingReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.ReportDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Branch).WithMany(p => p.KhoShiftClosingReports).HasConstraintName("fk_report_branch");

            entity.HasOne(d => d.User).WithMany(p => p.KhoShiftClosingReports).HasConstraintName("fk_report_user");
        });

        modelBuilder.Entity<KhoSupplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<LuongMonthlySalary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.LuongMonthlySalaries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("luong_monthly_salary_ibfk_1");
        });

        modelBuilder.Entity<LuongSalaryRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.AbsentPenalty).HasDefaultValueSql("'50000.00'");
            entity.Property(e => e.BonusAmount).HasDefaultValueSql("'300000.00'");
            entity.Property(e => e.BonusThresholdDays).HasDefaultValueSql("'15'");
            entity.Property(e => e.LatePenalty).HasDefaultValueSql("'20000.00'");
            entity.Property(e => e.WeekendMultiplier).HasDefaultValueSql("'1.5'");

            entity.HasOne(d => d.Branch).WithMany(p => p.LuongSalaryRules).HasConstraintName("luong_salary_rule_ibfk_1");
        });

        modelBuilder.Entity<NsRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.HourlyWage).HasDefaultValueSql("'0.00'");
            entity.Property(e => e.SeniorWage).HasDefaultValueSql("'0.00'");
        });

        modelBuilder.Entity<NsUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.HireDate).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.Branch).WithMany(p => p.NsUsers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_branch");

            entity.HasOne(d => d.Role).WithMany(p => p.NsUsers).HasConstraintName("fk_user_role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
