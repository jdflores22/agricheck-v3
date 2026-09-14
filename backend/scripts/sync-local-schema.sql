-- Sync LOCAL XAMPP DB to 83 application tables (matches production v3).
-- Run in local phpMyAdmin: ONE statement at a time. Skip "duplicate column/table" errors.
-- Or run: dotnet ef database update (from AgriCheck.Infrastructure project).

-- === 1. Entry columns ===
ALTER TABLE `entries` ADD `MavDocumentStatus` varchar(32) NOT NULL DEFAULT 'NotProvided';
ALTER TABLE `entries` ADD `MavNo` varchar(64) NULL;
ALTER TABLE `entries` ADD `MavRemarks` varchar(2000) NULL;
ALTER TABLE `entries` ADD `PrimaryMicId` bigint NULL;
ALTER TABLE `entries` ADD `ImportTrack` varchar(16) NOT NULL DEFAULT 'Regular';

-- === 2. Container columns ===
ALTER TABLE `containers` ADD `FormDataJson` json NULL;
ALTER TABLE `containers` ADD `SequenceNumber` int NOT NULL DEFAULT 1;
ALTER TABLE `containers` ADD `ContainerType` varchar(50) NULL;

-- === 3. Client bills link to agency billing ===
ALTER TABLE `client_bills` ADD `AgencyBillingId` bigint NULL;
CREATE UNIQUE INDEX `IX_client_bills_AgencyBillingId` ON `client_bills` (`AgencyBillingId`);
ALTER TABLE `client_bills`
  ADD CONSTRAINT `FK_client_bills_billings_AgencyBillingId`
  FOREIGN KEY (`AgencyBillingId`) REFERENCES `billings` (`Id`) ON DELETE SET NULL;

-- === 4. agency_payment_settings ===
CREATE TABLE IF NOT EXISTS `agency_payment_settings` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AgencyId` bigint NOT NULL,
  `PayMongoEnabled` tinyint(1) NOT NULL,
  `PayMongoApiKey` varchar(255) NULL,
  `PayMongoWebhookSecret` varchar(255) NULL,
  `PayMongoPublicKey` varchar(255) NULL,
  `CashPaymentEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CashPaymentInstructions` varchar(2000) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_agency_payment_settings_AgencyId` (`AgencyId`),
  CONSTRAINT `FK_agency_payment_settings_agencies_AgencyId`
    FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- === 5. billing_charges ===
CREATE TABLE IF NOT EXISTS `billing_charges` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
  `AgencyBillingId` bigint NOT NULL,
  `Description` varchar(256) NOT NULL,
  `Amount` decimal(12,2) NOT NULL,
  `SortOrder` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_billing_charges_Uuid` (`Uuid`),
  KEY `IX_billing_charges_AgencyBillingId` (`AgencyBillingId`),
  CONSTRAINT `FK_billing_charges_billings_AgencyBillingId`
    FOREIGN KEY (`AgencyBillingId`) REFERENCES `billings` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- === 6. inspector_assignments ===
CREATE TABLE IF NOT EXISTS `inspector_assignments` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
  `ContainerId` bigint NOT NULL,
  `AgencyId` bigint NOT NULL,
  `InspectorUserId` bigint NOT NULL,
  `Status` varchar(32) NOT NULL,
  `AssignedAt` datetime(6) NOT NULL,
  `CompletedAt` datetime(6) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_inspector_assignments_Uuid` (`Uuid`),
  KEY `IX_inspector_assignments_AgencyId` (`AgencyId`),
  KEY `IX_inspector_assignments_InspectorUserId` (`InspectorUserId`),
  KEY `IX_inspector_assignments_ContainerId_InspectorUserId_Status` (`ContainerId`, `InspectorUserId`, `Status`),
  CONSTRAINT `FK_inspector_assignments_agencies_AgencyId`
    FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_inspector_assignments_containers_ContainerId`
    FOREIGN KEY (`ContainerId`) REFERENCES `containers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_inspector_assignments_users_InspectorUserId`
    FOREIGN KEY (`InspectorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- === 7. operator_invite_codes (often missing on local) ===
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
  CONSTRAINT `FK_operator_invite_codes_users_OperatorUserId`
    FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- === 8. Mark migrations applied (optional) ===
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913010000_AgencyPaymentSettings', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260913010000_AgencyPaymentSettings'
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913020000_AgencyBillingCharges', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260913020000_AgencyBillingCharges'
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913030000_ClientBillAgencyBillingLink', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260913030000_ClientBillAgencyBillingLink'
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913040000_InspectorAssignments', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260913040000_InspectorAssignments'
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260913120000_DriverRegistrationAndInvites', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260913120000_DriverRegistrationAndInvites'
);

-- === 9. operator_vehicles + container vehicle assignment ===
CREATE TABLE IF NOT EXISTS `operator_vehicles` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
  `OperatorUserId` bigint NOT NULL,
  `PlateNumber` varchar(32) NOT NULL,
  `VehicleType` varchar(100) NOT NULL,
  `Description` varchar(255) NULL,
  `DefaultDriverUserId` bigint NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_operator_vehicles_Uuid` (`Uuid`),
  UNIQUE KEY `IX_operator_vehicles_OperatorUserId_PlateNumber` (`OperatorUserId`, `PlateNumber`),
  KEY `IX_operator_vehicles_DefaultDriverUserId` (`DefaultDriverUserId`),
  CONSTRAINT `FK_operator_vehicles_users_DefaultDriverUserId`
    FOREIGN KEY (`DefaultDriverUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_operator_vehicles_users_OperatorUserId`
    FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE `containers` ADD `AssignedOperatorVehicleId` bigint NULL;
CREATE INDEX `IX_containers_AssignedOperatorVehicleId` ON `containers` (`AssignedOperatorVehicleId`);
ALTER TABLE `containers`
  ADD CONSTRAINT `FK_containers_operator_vehicles_AssignedOperatorVehicleId`
  FOREIGN KEY (`AssignedOperatorVehicleId`) REFERENCES `operator_vehicles` (`Id`) ON DELETE SET NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260914130000_OperatorFleet', '8.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260914130000_OperatorFleet'
);

-- === Verify: expect 85 rows (84 app tables + __EFMigrationsHistory) ===
SHOW TABLES;
