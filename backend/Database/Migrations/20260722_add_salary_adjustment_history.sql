CREATE TABLE IF NOT EXISTS luong_salary_adjustment_history (
    id INT NOT NULL AUTO_INCREMENT,
    salary_id INT NOT NULL,
    user_id INT NOT NULL,
    created_by_user_id INT NOT NULL,
    month INT NOT NULL,
    year INT NOT NULL,
    bonus_amount DECIMAL(15, 2) NOT NULL DEFAULT 0,
    penalty_amount DECIMAL(15, 2) NOT NULL DEFAULT 0,
    reason VARCHAR(500) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_salary_adjustment_salary (salary_id),
    KEY idx_salary_adjustment_user_period (user_id, year, month),
    KEY idx_salary_adjustment_creator (created_by_user_id),
    CONSTRAINT fk_salary_adjustment_salary FOREIGN KEY (salary_id) REFERENCES luong_monthly_salary (id) ON DELETE CASCADE,
    CONSTRAINT fk_salary_adjustment_user FOREIGN KEY (user_id) REFERENCES ns_user (id),
    CONSTRAINT fk_salary_adjustment_creator FOREIGN KEY (created_by_user_id) REFERENCES ns_user (id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
