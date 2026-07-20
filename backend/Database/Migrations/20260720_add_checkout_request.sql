CREATE TABLE IF NOT EXISTS ca_checkout_request (
    id INT NOT NULL AUTO_INCREMENT,
    attendance_id INT NOT NULL,
    requested_by_user_id INT NOT NULL,
    proposed_check_out_time DATETIME NOT NULL,
    requested_check_out_time DATETIME NULL,
    reason VARCHAR(500) NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'AWAITING_EMPLOYEE',
    reviewed_by_user_id INT NULL,
    reject_reason VARCHAR(500) NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL,
    reviewed_at DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY idx_checkout_request_attendance (attendance_id),
    KEY idx_checkout_request_status (status, updated_at),
    KEY idx_checkout_request_user (requested_by_user_id),
    KEY idx_checkout_request_reviewer (reviewed_by_user_id),
    CONSTRAINT fk_checkout_request_attendance FOREIGN KEY (attendance_id) REFERENCES ca_attendance (id) ON DELETE CASCADE,
    CONSTRAINT fk_checkout_request_user FOREIGN KEY (requested_by_user_id) REFERENCES ns_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_checkout_request_reviewer FOREIGN KEY (reviewed_by_user_id) REFERENCES ns_user (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ca_checkout_request_history (
    id INT NOT NULL AUTO_INCREMENT,
    request_id INT NOT NULL,
    actor_user_id INT NULL,
    action VARCHAR(30) NOT NULL,
    detail VARCHAR(1000) NULL,
    created_at DATETIME NOT NULL,
    PRIMARY KEY (id),
    KEY idx_checkout_history_request (request_id),
    KEY idx_checkout_history_actor (actor_user_id),
    CONSTRAINT fk_checkout_history_request FOREIGN KEY (request_id) REFERENCES ca_checkout_request (id) ON DELETE CASCADE,
    CONSTRAINT fk_checkout_history_actor FOREIGN KEY (actor_user_id) REFERENCES ns_user (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
