-- Manual fallback for production MySQL if EF migration did not run yet.
-- Safe to run once; skip if operator_invite_codes already exists.

CREATE TABLE IF NOT EXISTS `operator_invite_codes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Code` varchar(32) NOT NULL,
  `OperatorUserId` bigint NOT NULL,
  `Label` varchar(128) NULL,
  `MaxUses` int NOT NULL,
  `UsedCount` int NOT NULL,
  `ExpiresAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_operator_invite_codes_Code` (`Code`),
  KEY `IX_operator_invite_codes_OperatorUserId` (`OperatorUserId`),
  CONSTRAINT `FK_operator_invite_codes_users_OperatorUserId` FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET @has_birthdate := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'BirthDate'
);
SET @sql_birthdate := IF(@has_birthdate = 0, 'ALTER TABLE `driver_profiles` ADD `BirthDate` datetime(6) NULL', 'SELECT 1');
PREPARE stmt FROM @sql_birthdate; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_region := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'RegionId'
);
SET @sql_region := IF(@has_region = 0, 'ALTER TABLE `driver_profiles` ADD `RegionId` bigint NULL', 'SELECT 1');
PREPARE stmt FROM @sql_region; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_province := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'ProvinceId'
);
SET @sql_province := IF(@has_province = 0, 'ALTER TABLE `driver_profiles` ADD `ProvinceId` bigint NULL', 'SELECT 1');
PREPARE stmt FROM @sql_province; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_city := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'CityId'
);
SET @sql_city := IF(@has_city = 0, 'ALTER TABLE `driver_profiles` ADD `CityId` bigint NULL', 'SELECT 1');
PREPARE stmt FROM @sql_city; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_barangay := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'BarangayId'
);
SET @sql_barangay := IF(@has_barangay = 0, 'ALTER TABLE `driver_profiles` ADD `BarangayId` bigint NULL', 'SELECT 1');
PREPARE stmt FROM @sql_barangay; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_zip := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'ZipCode'
);
SET @sql_zip := IF(@has_zip = 0, 'ALTER TABLE `driver_profiles` ADD `ZipCode` varchar(16) NULL', 'SELECT 1');
PREPARE stmt FROM @sql_zip; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_street := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'StreetAddress'
);
SET @sql_street := IF(@has_street = 0, 'ALTER TABLE `driver_profiles` ADD `StreetAddress` varchar(255) NULL', 'SELECT 1');
PREPARE stmt FROM @sql_street; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_operator := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND COLUMN_NAME = 'OperatorUserId'
);
SET @sql_operator := IF(@has_operator = 0, 'ALTER TABLE `driver_profiles` ADD `OperatorUserId` bigint NULL', 'SELECT 1');
PREPARE stmt FROM @sql_operator; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_operator_idx := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'driver_profiles' AND INDEX_NAME = 'IX_driver_profiles_OperatorUserId'
);
SET @sql_operator_idx := IF(@has_operator_idx = 0, 'CREATE INDEX `IX_driver_profiles_OperatorUserId` ON `driver_profiles` (`OperatorUserId`)', 'SELECT 1');
PREPARE stmt FROM @sql_operator_idx; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913120000_DriverRegistrationAndInvites', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260913120000_DriverRegistrationAndInvites'
);
