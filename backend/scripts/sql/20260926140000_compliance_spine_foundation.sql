-- P8.1 Compliance spine (manual apply if ef database update is behind)
-- Safe to re-run: checks information_schema before ALTER/CREATE.

SET @db := DATABASE();

SET @has_phase := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'entries' AND COLUMN_NAME = 'CompliancePhase'
);
SET @sql_phase := IF(@has_phase = 0,
  'ALTER TABLE `entries` ADD `CompliancePhase` varchar(32) NOT NULL DEFAULT ''Draft''',
  'SELECT 1');
PREPARE stmt FROM @sql_phase;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_wh := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'entries' AND COLUMN_NAME = 'FinalWarehouseFacilityId'
);
SET @sql_wh := IF(@has_wh = 0,
  'ALTER TABLE `entries` ADD `FinalWarehouseFacilityId` bigint NULL',
  'SELECT 1');
PREPARE stmt FROM @sql_wh;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_idx := (
  SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'entries' AND INDEX_NAME = 'IX_entries_FinalWarehouseFacilityId'
);
SET @sql_idx := IF(@has_idx = 0,
  'CREATE INDEX `IX_entries_FinalWarehouseFacilityId` ON `entries` (`FinalWarehouseFacilityId`)',
  'SELECT 1');
PREPARE stmt FROM @sql_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_fk := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'entries'
    AND CONSTRAINT_NAME = 'FK_entries_warehouse_facilities_FinalWarehouseFacilityId'
);
SET @sql_fk := IF(@has_fk = 0,
  'ALTER TABLE `entries` ADD CONSTRAINT `FK_entries_warehouse_facilities_FinalWarehouseFacilityId` FOREIGN KEY (`FinalWarehouseFacilityId`) REFERENCES `warehouse_facilities` (`Id`) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt FROM @sql_fk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS `compliance_sync_outbox` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `EntityType` varchar(64) NOT NULL,
  `EntityId` bigint NOT NULL,
  `TargetSystem` varchar(32) NOT NULL,
  `Operation` varchar(64) NOT NULL,
  `PayloadJson` longtext NOT NULL,
  `Status` varchar(16) NOT NULL,
  `AttemptCount` int NOT NULL,
  `LastError` varchar(2000) NULL,
  `SyncedAt` datetime(6) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_compliance_sync_outbox_Status_CreatedAt` (`Status`, `CreatedAt`)
) CHARACTER SET=utf8mb4;
