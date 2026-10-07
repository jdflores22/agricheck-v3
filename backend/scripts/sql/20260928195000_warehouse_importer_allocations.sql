-- warehouse_importer_allocations + demo importer contract at WH-MNL (150 MT, fish scope)
SET @db := DATABASE();

SET @has := (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'warehouse_importer_allocations');
SET @sql := IF(@has = 0,
  'CREATE TABLE `warehouse_importer_allocations` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
    `ImporterUserId` bigint NOT NULL,
    `WarehouseFacilityId` bigint NOT NULL,
    `ContractReference` varchar(128) NOT NULL,
    `AllocatedCapacityKg` decimal(18,3) NOT NULL,
    `AllowedCommodityClassesJson` longtext NOT NULL,
    `ValidFrom` datetime(6) NOT NULL,
    `ValidTo` datetime(6) NULL,
    `Status` varchar(16) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_warehouse_importer_allocations_Uuid` (`Uuid`),
    KEY `IX_warehouse_importer_allocations_ImporterUserId_WarehouseFacilityId_Status` (`ImporterUserId`,`WarehouseFacilityId`,`Status`),
    CONSTRAINT `FK_wh_imp_alloc_importer` FOREIGN KEY (`ImporterUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_wh_imp_alloc_facility` FOREIGN KEY (`WarehouseFacilityId`) REFERENCES `warehouse_facilities` (`Id`)
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260928195000_WarehouseImporterAllocations', '7.0.20'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928195000_WarehouseImporterAllocations'
);

SET @importer_id := (SELECT Id FROM users WHERE Email = 'importer@agricheck.local' LIMIT 1);
SET @wh_id := (SELECT Id FROM warehouse_facilities WHERE Code = 'WH-MNL' LIMIT 1);

UPDATE facility_registrations
SET ProductsJson = '["FishAndAquaticProducts"]',
    MaxCapacityKg = COALESCE(MaxCapacityKg, 500000)
WHERE WarehouseFacilityId = @wh_id
  AND (ProductsJson IS NULL OR ProductsJson = '' OR ProductsJson = '[]');

INSERT INTO warehouse_importer_allocations (
  Uuid, ImporterUserId, WarehouseFacilityId, ContractReference,
  AllocatedCapacityKg, AllowedCommodityClassesJson, ValidFrom, ValidTo, Status, CreatedAt, UpdatedAt
)
SELECT UUID(), @importer_id, @wh_id, 'DEMO-MNL-150MT', 150000,
  '["FishAndAquaticProducts"]', UTC_TIMESTAMP(), NULL, 'Active', UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE @importer_id IS NOT NULL AND @wh_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM warehouse_importer_allocations
    WHERE ImporterUserId = @importer_id AND WarehouseFacilityId = @wh_id AND Status = 'Active'
  );

INSERT INTO importer_facility_affiliations (Uuid, UserId, WarehouseFacilityId, CreatedAt, UpdatedAt)
SELECT UUID(), @importer_id, @wh_id, UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE @importer_id IS NOT NULL AND @wh_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM importer_facility_affiliations
    WHERE UserId = @importer_id AND WarehouseFacilityId = @wh_id
  );
