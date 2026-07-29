-- Luồng mới:
-- Manager chốt -> nhân viên xem/khiếu nại -> Manager xử lý -> Manager trả lương.
-- Không còn bước Admin chốt bảng lương.
--
-- Nên sao lưu bảng luong_monthly_salary trước khi chạy vì ALTER TABLE
-- trong MySQL tự commit và không thể rollback bằng transaction.

-- Giữ nguyên các bảng lương đã được Admin chốt trước đây ở trạng thái
-- Manager đã chốt để Manager có thể tiếp tục xác nhận thanh toán.
UPDATE luong_monthly_salary
SET status = 'FINALIZED'
WHERE UPPER(COALESCE(status, '')) = 'ADMIN_FINALIZED';

ALTER TABLE luong_monthly_salary
    DROP FOREIGN KEY fk_monthly_salary_admin_finalized_by;

ALTER TABLE luong_monthly_salary
    DROP INDEX idx_monthly_salary_admin_finalized_by;

ALTER TABLE luong_monthly_salary
    DROP COLUMN admin_finalized_at,
    DROP COLUMN admin_finalized_by_user_id;
