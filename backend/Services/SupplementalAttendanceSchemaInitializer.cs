using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public static class SupplementalAttendanceSchemaInitializer
{
    public static Task InitializeAsync(AppDbContext context) => context.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `ca_supplemental_attendance_request` (
          `id` int NOT NULL AUTO_INCREMENT,
          `schedule_id` int NOT NULL,
          `requested_by_manager_id` int NOT NULL,
          `proposed_check_in_time` datetime NOT NULL,
          `proposed_check_out_time` datetime NOT NULL,
          `reason` varchar(500) DEFAULT NULL,
          `status` varchar(20) NOT NULL DEFAULT 'PENDING',
          `reviewed_by_admin_id` int DEFAULT NULL,
          `reject_reason` varchar(500) DEFAULT NULL,
          `created_at` datetime NOT NULL,
          `updated_at` datetime NOT NULL,
          `reviewed_at` datetime DEFAULT NULL,
          PRIMARY KEY (`id`),
          UNIQUE KEY `uq_supplemental_schedule` (`schedule_id`),
          KEY `idx_supplemental_status` (`status`,`updated_at`),
          KEY `idx_supplemental_manager` (`requested_by_manager_id`),
          KEY `idx_supplemental_admin` (`reviewed_by_admin_id`),
          CONSTRAINT `fk_supplemental_schedule` FOREIGN KEY (`schedule_id`) REFERENCES `ca_final_schedule` (`id`) ON DELETE CASCADE,
          CONSTRAINT `fk_supplemental_manager` FOREIGN KEY (`requested_by_manager_id`) REFERENCES `ns_user` (`id`) ON DELETE RESTRICT,
          CONSTRAINT `fk_supplemental_admin` FOREIGN KEY (`reviewed_by_admin_id`) REFERENCES `ns_user` (`id`) ON DELETE SET NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """);
}
