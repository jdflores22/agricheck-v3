-- Link MIC utilization to issued SPSIC (idempotent).

SET @db := DATABASE();
SET @has := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'mic_utilizations' AND COLUMN_NAME = 'SpsImportClearanceId');
SET @sql := IF(@has = 0,
  'ALTER TABLE `mic_utilizations` ADD COLUMN `SpsImportClearanceId` bigint NULL, ADD INDEX `IX_mic_utilizations_SpsImportClearanceId` (`SpsImportClearanceId`), ADD CONSTRAINT `FK_mic_utilizations_sps_import_clearances_SpsImportClearanceId` FOREIGN KEY (`SpsImportClearanceId`) REFERENCES `sps_import_clearances` (`Id`) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
