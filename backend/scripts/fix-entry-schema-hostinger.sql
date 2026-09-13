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
