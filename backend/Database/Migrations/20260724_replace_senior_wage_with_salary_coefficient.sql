ALTER TABLE ns_user
    ADD COLUMN salary_coefficient DECIMAL(5, 2) NOT NULL DEFAULT 1.00,
    ADD COLUMN salary_coefficient_is_manual TINYINT(1) NOT NULL DEFAULT 0;

UPDATE ns_user
SET salary_coefficient = CASE
    WHEN hire_date IS NULL OR CURDATE() < DATE_ADD(hire_date, INTERVAL 6 MONTH) THEN 1.00
    WHEN CURDATE() < DATE_ADD(hire_date, INTERVAL 12 MONTH) THEN 1.20
    ELSE 1.50
END;

ALTER TABLE ns_role
    DROP COLUMN senior_wage;
