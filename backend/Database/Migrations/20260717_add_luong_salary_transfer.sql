CREATE TABLE IF NOT EXISTS luong_salary_transfer (
    id INT NOT NULL AUTO_INCREMENT,
    branch_id INT NOT NULL,
    manager_id INT NOT NULL,
    transferred_by_user_id INT NOT NULL,
    month INT NOT NULL,
    year INT NOT NULL,
    salary_count INT NOT NULL DEFAULT 0,
    total_amount DECIMAL(15, 2) NOT NULL DEFAULT 0,
    transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_salary_transfer_branch_period (branch_id, month, year),
    KEY idx_salary_transfer_manager (manager_id),
    KEY idx_salary_transfer_admin (transferred_by_user_id),
    CONSTRAINT fk_salary_transfer_branch FOREIGN KEY (branch_id) REFERENCES dm_branch (id),
    CONSTRAINT fk_salary_transfer_manager FOREIGN KEY (manager_id) REFERENCES ns_user (id),
    CONSTRAINT fk_salary_transfer_admin FOREIGN KEY (transferred_by_user_id) REFERENCES ns_user (id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
