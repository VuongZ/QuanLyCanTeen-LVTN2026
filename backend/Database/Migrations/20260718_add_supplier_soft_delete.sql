ALTER TABLE kho_supplier
    ADD COLUMN is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN deleted_at TIMESTAMP NULL DEFAULT NULL;

CREATE INDEX idx_kho_supplier_is_deleted ON kho_supplier (is_deleted);
