-- P8.3 RSAS / CSW facility tables (idempotent). Run after agency_license script.

SET @db := DATABASE();

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'facility_registrations');
SET @sql := IF(@has = 0,
  'CREATE TABLE `facility_registrations` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) NOT NULL,
    `WarehouseFacilityId` bigint NOT NULL,
    `SubmittedByUserId` bigint NOT NULL,
    `Status` varchar(16) NOT NULL,
    `RsasExternalRef` varchar(64) NULL,
    `OwnerName` varchar(256) NOT NULL,
    `MaxCapacityKg` decimal(18,2) NULL,
    `ProductsJson` longtext NULL,
    `FormDataJson` longtext NULL,
    `RegisteredAt` datetime(6) NULL,
    `ExpiresAt` datetime(6) NULL,
    `ReviewComments` varchar(2000) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_facility_registrations_Uuid` (`Uuid`),
    UNIQUE KEY `IX_facility_registrations_WarehouseFacilityId` (`WarehouseFacilityId`),
    CONSTRAINT `FK_facility_registrations_warehouse_facilities_WarehouseFacilityId` FOREIGN KEY (`WarehouseFacilityId`) REFERENCES `warehouse_facilities` (`Id`),
    CONSTRAINT `FK_facility_registrations_users_SubmittedByUserId` FOREIGN KEY (`SubmittedByUserId`) REFERENCES `users` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'facility_accreditations');
SET @sql := IF(@has = 0,
  'CREATE TABLE `facility_accreditations` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) NOT NULL,
    `FacilityRegistrationId` bigint NOT NULL,
    `WarehouseFacilityId` bigint NOT NULL,
    `Status` varchar(16) NOT NULL,
    `CertificateNumber` varchar(64) NULL,
    `LeadAgencyId` bigint NULL,
    `AccreditedAt` datetime(6) NULL,
    `ExpiresAt` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_facility_accreditations_Uuid` (`Uuid`),
    KEY `IX_facility_accreditations_WarehouseFacilityId` (`WarehouseFacilityId`),
    CONSTRAINT `FK_facility_accreditations_facility_registrations_FacilityRegistrationId` FOREIGN KEY (`FacilityRegistrationId`) REFERENCES `facility_registrations` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_facility_accreditations_warehouse_facilities_WarehouseFacilityId` FOREIGN KEY (`WarehouseFacilityId`) REFERENCES `warehouse_facilities` (`Id`),
    CONSTRAINT `FK_facility_accreditations_agencies_LeadAgencyId` FOREIGN KEY (`LeadAgencyId`) REFERENCES `agencies` (`Id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
