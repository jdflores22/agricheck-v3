-- P8.2 Agency license tables (idempotent). Run after compliance_spine_foundation.sql.

SET @db := DATABASE();

SET @has_types := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'agency_license_types'
);
SET @sql := IF(@has_types = 0,
  'CREATE TABLE `agency_license_types` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `AgencyId` bigint NOT NULL,
    `Code` varchar(32) NOT NULL,
    `Name` varchar(128) NOT NULL,
    `Kind` varchar(32) NOT NULL,
    `IsRequiredForSpsic` tinyint(1) NOT NULL DEFAULT 1,
    `Description` varchar(512) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_agency_license_types_AgencyId_Code` (`AgencyId`,`Code`),
    CONSTRAINT `FK_agency_license_types_agencies_AgencyId` FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_apps := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'agency_license_applications'
);
SET @sql := IF(@has_apps = 0,
  'CREATE TABLE `agency_license_applications` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) NOT NULL,
    `UserId` bigint NOT NULL,
    `AgencyId` bigint NOT NULL,
    `AgencyLicenseTypeId` bigint NOT NULL,
    `Status` varchar(16) NOT NULL,
    `FormDataJson` longtext NULL,
    `SubmittedAt` datetime(6) NULL,
    `ReviewComments` varchar(2000) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_agency_license_applications_Uuid` (`Uuid`),
    KEY `FK_agency_license_applications_users_UserId` (`UserId`),
    KEY `FK_agency_license_applications_agencies_AgencyId` (`AgencyId`),
    KEY `FK_agency_license_applications_agency_license_types_AgencyLicenseTypeId` (`AgencyLicenseTypeId`),
    CONSTRAINT `FK_agency_license_applications_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`),
    CONSTRAINT `FK_agency_license_applications_agencies_AgencyId` FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`),
    CONSTRAINT `FK_agency_license_applications_agency_license_types_AgencyLicenseTypeId` FOREIGN KEY (`AgencyLicenseTypeId`) REFERENCES `agency_license_types` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_lic := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'agency_licenses'
);
SET @sql := IF(@has_lic = 0,
  'CREATE TABLE `agency_licenses` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) NOT NULL,
    `UserId` bigint NOT NULL,
    `AgencyId` bigint NOT NULL,
    `AgencyLicenseTypeId` bigint NOT NULL,
    `ApplicationId` bigint NULL,
    `LicenseNumber` varchar(64) NOT NULL,
    `Status` varchar(16) NOT NULL,
    `IssuedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `Notes` varchar(2000) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_agency_licenses_Uuid` (`Uuid`),
    KEY `IX_agency_licenses_UserId_AgencyId_Status` (`UserId`,`AgencyId`,`Status`),
    CONSTRAINT `FK_agency_licenses_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`),
    CONSTRAINT `FK_agency_licenses_agencies_AgencyId` FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`),
    CONSTRAINT `FK_agency_licenses_agency_license_types_AgencyLicenseTypeId` FOREIGN KEY (`AgencyLicenseTypeId`) REFERENCES `agency_license_types` (`Id`),
    CONSTRAINT `FK_agency_licenses_agency_license_applications_ApplicationId` FOREIGN KEY (`ApplicationId`) REFERENCES `agency_license_applications` (`Id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rules := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'commodity_license_rules'
);
SET @sql := IF(@has_rules = 0,
  'CREATE TABLE `commodity_license_rules` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `AgencyId` bigint NOT NULL,
    `AgencyLicenseTypeId` bigint NOT NULL,
    `CommodityId` bigint NULL,
    `HsCodePrefix` varchar(16) NULL,
    `IsRequired` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_commodity_license_rules_agencies_AgencyId` FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`),
    CONSTRAINT `FK_commodity_license_rules_agency_license_types_AgencyLicenseTypeId` FOREIGN KEY (`AgencyLicenseTypeId`) REFERENCES `agency_license_types` (`Id`),
    CONSTRAINT `FK_commodity_license_rules_commodities_CommodityId` FOREIGN KEY (`CommodityId`) REFERENCES `commodities` (`Id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Seed LTO/LTI types for BAI, BPI, BFAR (restart API seeder also applies this).
INSERT INTO agency_license_types (`AgencyId`, `Code`, `Name`, `Kind`, `IsRequiredForSpsic`, `CreatedAt`, `UpdatedAt`)
SELECT a.Id, t.Code, t.Name, t.Kind, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM agencies a
CROSS JOIN (
  SELECT 'LTO' AS Code, 'License to Operate' AS Name, 'Lto' AS Kind
  UNION ALL SELECT 'LTI', 'License to Import', 'Lti'
) t
WHERE a.Code IN ('BAI','BPI','BFAR')
  AND NOT EXISTS (
    SELECT 1 FROM agency_license_types alt WHERE alt.AgencyId = a.Id AND alt.Code = t.Code
  );
