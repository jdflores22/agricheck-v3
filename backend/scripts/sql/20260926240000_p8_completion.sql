-- P8 completion: inspections extensions, TIS/TOS, must-ship-out rules, prerequisites, BMBE flag
SET @db = DATABASE();

-- inspections.Kind + compliance FKs
SET @sql = (SELECT IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'inspections' AND COLUMN_NAME = 'Kind') = 0,
  'ALTER TABLE `inspections` ADD COLUMN `Kind` varchar(32) NOT NULL DEFAULT ''Standard''',
  'SELECT 1'));
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- (Additional columns/tables: run EF migration 20260926240000_P8Completion or mirror Up() from that migration in phpMyAdmin.)
