-- Run in Hostinger phpMyAdmin (skip any statement that says "duplicate column").
-- Fixes HTTP 500 when saving entries on older production databases.

ALTER TABLE `entries` ADD `MavDocumentStatus` varchar(32) NOT NULL DEFAULT 'NotProvided';
ALTER TABLE `entries` ADD `MavNo` varchar(64) NULL;
ALTER TABLE `entries` ADD `MavRemarks` varchar(2000) NULL;
ALTER TABLE `entries` ADD `PrimaryMicId` bigint NULL;
ALTER TABLE `entries` ADD `ImportTrack` varchar(16) NOT NULL DEFAULT 'Regular';

ALTER TABLE `containers` ADD `FormDataJson` json NULL;
ALTER TABLE `containers` ADD `SequenceNumber` int NOT NULL DEFAULT 1;
ALTER TABLE `containers` ADD `ContainerType` varchar(50) NULL;

-- Billing schema (fixes "Unknown column 'c.AgencyBillingId'")
ALTER TABLE `client_bills` ADD `AgencyBillingId` bigint NULL;
CREATE UNIQUE INDEX `IX_client_bills_AgencyBillingId` ON `client_bills` (`AgencyBillingId`);

-- Agency PayMongo settings (fixes payment-options / initiate 500)
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
