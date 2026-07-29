CREATE TABLE luong_salary_complaint (
    id INT NOT NULL AUTO_INCREMENT,
    salary_id INT NOT NULL,
    user_id INT NOT NULL,
    content VARCHAR(1000) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    manager_response VARCHAR(1000) NULL,
    reviewed_by_user_id INT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reviewed_at DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_salary_complaint_salary (salary_id),
    KEY idx_salary_complaint_user (user_id),
    KEY idx_salary_complaint_reviewer (reviewed_by_user_id),
    KEY idx_salary_complaint_status (status),
    CONSTRAINT fk_salary_complaint_salary
        FOREIGN KEY (salary_id) REFERENCES luong_monthly_salary (id)
        ON DELETE CASCADE,
    CONSTRAINT fk_salary_complaint_user
        FOREIGN KEY (user_id) REFERENCES ns_user (id),
    CONSTRAINT fk_salary_complaint_reviewer
        FOREIGN KEY (reviewed_by_user_id) REFERENCES ns_user (id)
        ON DELETE SET NULL
);
