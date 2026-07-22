ALTER TABLE luong_monthly_salary
    ADD COLUMN finalized_at DATETIME NULL AFTER paid_at,
    ADD COLUMN finalized_by_user_id INT NULL AFTER finalized_at,
    ADD KEY idx_monthly_salary_finalized_by (finalized_by_user_id),
    ADD CONSTRAINT fk_monthly_salary_finalized_by
        FOREIGN KEY (finalized_by_user_id) REFERENCES ns_user (id)
        ON DELETE SET NULL;
