ALTER TABLE luong_monthly_salary
    ADD COLUMN admin_finalized_at DATETIME NULL AFTER finalized_by_user_id,
    ADD COLUMN admin_finalized_by_user_id INT NULL AFTER admin_finalized_at,
    ADD KEY idx_monthly_salary_admin_finalized_by (admin_finalized_by_user_id),
    ADD CONSTRAINT fk_monthly_salary_admin_finalized_by
        FOREIGN KEY (admin_finalized_by_user_id) REFERENCES ns_user (id)
        ON DELETE SET NULL;
