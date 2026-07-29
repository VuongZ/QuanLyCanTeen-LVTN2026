ALTER TABLE luong_salary_adjustment_history
    ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'APPROVED' AFTER reason,
    ADD COLUMN reviewed_by_user_id INT NULL AFTER status,
    ADD COLUMN reviewed_at DATETIME NULL AFTER reviewed_by_user_id,
    ADD COLUMN review_note VARCHAR(500) NULL AFTER reviewed_at;

ALTER TABLE luong_salary_adjustment_history
    MODIFY COLUMN status VARCHAR(20) NOT NULL DEFAULT 'PENDING';

CREATE INDEX idx_salary_adjustment_status
    ON luong_salary_adjustment_history (status);

CREATE INDEX idx_salary_adjustment_reviewer
    ON luong_salary_adjustment_history (reviewed_by_user_id);

ALTER TABLE luong_salary_adjustment_history
    ADD CONSTRAINT fk_salary_adjustment_reviewer
        FOREIGN KEY (reviewed_by_user_id)
        REFERENCES ns_user (id)
        ON DELETE SET NULL;
