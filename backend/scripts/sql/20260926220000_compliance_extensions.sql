-- P8 SPS checklist, issuing rules, e-RFI, container clearance FK (idempotent). Run after SPSIC foundation.

SET @db := DATABASE();

SET @has := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'containers' AND COLUMN_NAME = 'SpsImportClearanceId');
SET @sql := IF(@has = 0,
  'ALTER TABLE `containers` ADD COLUMN `SpsImportClearanceId` bigint NULL, ADD INDEX `IX_containers_SpsImportClearanceId` (`SpsImportClearanceId`), ADD CONSTRAINT `FK_containers_sps_import_clearances_SpsImportClearanceId` FOREIGN KEY (`SpsImportClearanceId`) REFERENCES `sps_import_clearances` (`Id`) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'sps_document_checklist_templates');
SET @sql := IF(@has = 0,
  'CREATE TABLE `sps_document_checklist_templates` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Code` varchar(8) NOT NULL,
    `Title` varchar(256) NOT NULL,
    `SortOrder` int NOT NULL,
    `IsRequired` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_sps_document_checklist_templates_Code` (`Code`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'sps_issuing_rules');
SET @sql := IF(@has = 0,
  'CREATE TABLE `sps_issuing_rules` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `HsCodePrefix` varchar(16) NOT NULL,
    `IssuingAgencyId` bigint NOT NULL,
    `IsPrimaryIssuer` tinyint(1) NOT NULL,
    `Notes` varchar(512) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_sps_issuing_rules_HsCodePrefix_IssuingAgencyId` (`HsCodePrefix`, `IssuingAgencyId`),
    CONSTRAINT `FK_sps_issuing_rules_agencies_IssuingAgencyId` FOREIGN KEY (`IssuingAgencyId`) REFERENCES `agencies` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'sps_application_documents');
SET @sql := IF(@has = 0,
  'CREATE TABLE `sps_application_documents` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ApplicationId` bigint NOT NULL,
    `DocumentCode` varchar(8) NOT NULL,
    `Status` varchar(16) NOT NULL,
    `EntryFileId` bigint NULL,
    `Notes` varchar(512) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_sps_application_documents_ApplicationId_DocumentCode` (`ApplicationId`, `DocumentCode`),
    CONSTRAINT `FK_sps_application_documents_sps_import_clearance_applications_ApplicationId` FOREIGN KEY (`ApplicationId`) REFERENCES `sps_import_clearance_applications` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_sps_application_documents_entry_files_EntryFileId` FOREIGN KEY (`EntryFileId`) REFERENCES `entry_files` (`Id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'erfi_submissions');
SET @sql := IF(@has = 0,
  'CREATE TABLE `erfi_submissions` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) NOT NULL,
    `EntryId` bigint NOT NULL,
    `SpsImportClearanceId` bigint NOT NULL,
    `FiledByUserId` bigint NOT NULL,
    `Status` varchar(16) NOT NULL,
    `FiledAt` datetime(6) NULL,
    `EtaAtPoe` datetime(6) NULL,
    `BolReference` varchar(64) NULL,
    `IfmDestinationJson` longtext NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_erfi_submissions_Uuid` (`Uuid`),
    UNIQUE KEY `IX_erfi_submissions_EntryId` (`EntryId`),
    CONSTRAINT `FK_erfi_submissions_entries_EntryId` FOREIGN KEY (`EntryId`) REFERENCES `entries` (`Id`),
    CONSTRAINT `FK_erfi_submissions_sps_import_clearances_SpsImportClearanceId` FOREIGN KEY (`SpsImportClearanceId`) REFERENCES `sps_import_clearances` (`Id`),
    CONSTRAINT `FK_erfi_submissions_users_FiledByUserId` FOREIGN KEY (`FiledByUserId`) REFERENCES `users` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
