-- Migration: host status tracking (FR017 - host reachability indicator & filtering)
-- up
-- Use INFORMATION_SCHEMA guards for idempotency (works even if linter doesn't understand IF NOT EXISTS)

-- is_reachable
SET @need_is_reachable := (
	SELECT COUNT(*) = 0 FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME='hosts' AND COLUMN_NAME='is_reachable'
);
SET @stmt := IF(@need_is_reachable, 'ALTER TABLE hosts ADD COLUMN is_reachable TINYINT(1) NOT NULL DEFAULT 0', 'DO 0');
PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;

-- last_checked_utc
SET @need_last_checked := (
	SELECT COUNT(*) = 0 FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME='hosts' AND COLUMN_NAME='last_checked_utc'
);
SET @stmt := IF(@need_last_checked, 'ALTER TABLE hosts ADD COLUMN last_checked_utc DATETIME(6) NULL', 'DO 0');
PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;

-- last_reachable_utc
SET @need_last_reach := (
	SELECT COUNT(*) = 0 FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME='hosts' AND COLUMN_NAME='last_reachable_utc'
);
SET @stmt := IF(@need_last_reach, 'ALTER TABLE hosts ADD COLUMN last_reachable_utc DATETIME(6) NULL', 'DO 0');
PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
-- down (manual rollback example)
-- ALTER TABLE hosts DROP COLUMN is_reachable, DROP COLUMN last_checked_utc, DROP COLUMN last_reachable_utc;
-- ALTER TABLE hosts DROP COLUMN is_reachable, DROP COLUMN last_checked_utc, DROP COLUMN last_reachable_utc;
