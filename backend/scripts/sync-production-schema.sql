-- Sync Hostinger production DB to match local v3 schema (83 application tables).
-- Run in phpMyAdmin. Skip statements that report "duplicate column/table".

-- Entry / container columns
ALTER TABLE `entries` ADD `MavDocumentStatus` varchar(32) NOT NULL DEFAULT 'NotProvided';
ALTER TABLE `entries` ADD `MavNo` varchar(64) NULL;
ALTER TABLE `entries` ADD `MavRemarks` varchar(2000) NULL;
ALTER TABLE `entries` ADD `PrimaryMicId` bigint NULL;
ALTER TABLE `entries` ADD `ImportTrack` varchar(16) NOT NULL DEFAULT 'Regular';
ALTER TABLE `containers` ADD `FormDataJson` json NULL;
ALTER TABLE `containers` ADD `SequenceNumber` int NOT NULL DEFAULT 1;
ALTER TABLE `containers` ADD `ContainerType` varchar(50) NULL;

-- Agency payment settings (per-agency PayMongo)
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

-- Billing charges
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

-- Client bill agency billing link
ALTER TABLE `client_bills` ADD `AgencyBillingId` bigint NULL;
CREATE UNIQUE INDEX `IX_client_bills_AgencyBillingId` ON `client_bills` (`AgencyBillingId`);

-- Inspector assignments
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

-- Operator invite codes (AgriTrack driver registration)
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

-- Verify table count (expect 83, excluding __EFMigrationsHistory)
SELECT COUNT(*) AS application_table_count
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_type = 'BASE TABLE'
  AND table_name != '__EFMigrationsHistory';
