-- Warehouse staff assignments + LTO application queue (run after 20260928120000)

CREATE TABLE IF NOT EXISTS warehouse_staff_assignments (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    UserId BIGINT NOT NULL,
    WarehouseFacilityId BIGINT NOT NULL,
    Role VARCHAR(32) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_warehouse_staff_assignments_UserId_WarehouseFacilityId (UserId, WarehouseFacilityId),
    CONSTRAINT FK_wh_staff_assign_user FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_wh_staff_assign_facility FOREIGN KEY (WarehouseFacilityId) REFERENCES warehouse_facilities (Id) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS warehouse_operating_license_applications (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    Uuid CHAR(36) NOT NULL COLLATE ascii_general_ci,
    WarehouseFacilityId BIGINT NOT NULL,
    SubmittedByUserId BIGINT NOT NULL,
    OperatorLegalName VARCHAR(256) NOT NULL,
    ContactEmail VARCHAR(256) NULL,
    ContactPhone VARCHAR(64) NULL,
    Notes VARCHAR(2000) NULL,
    Status VARCHAR(16) NOT NULL,
    SubmittedAt DATETIME(6) NOT NULL,
    ReviewedByUserId BIGINT NULL,
    ReviewedAt DATETIME(6) NULL,
    ReviewComments VARCHAR(2000) NULL,
    IssuedLicenseId BIGINT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_warehouse_operating_license_applications_Uuid (Uuid),
    KEY IX_warehouse_operating_license_applications_WarehouseFacilityId (WarehouseFacilityId),
    CONSTRAINT FK_wh_lto_app_submitter FOREIGN KEY (SubmittedByUserId) REFERENCES users (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_wh_lto_app_reviewer FOREIGN KEY (ReviewedByUserId) REFERENCES users (Id) ON DELETE SET NULL,
    CONSTRAINT FK_wh_lto_app_facility FOREIGN KEY (WarehouseFacilityId) REFERENCES warehouse_facilities (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_wh_lto_app_license FOREIGN KEY (IssuedLicenseId) REFERENCES warehouse_operating_licenses (Id) ON DELETE SET NULL
) CHARACTER SET utf8mb4;

-- Demo warehouse staff → Manila Cold Storage (WH-MNL) only
DELETE a FROM warehouse_staff_assignments a
INNER JOIN users u ON u.Id = a.UserId
INNER JOIN warehouse_facilities w ON w.Id = a.WarehouseFacilityId
WHERE u.Email = 'warehouse@agricheck.local'
  AND w.Code <> 'WH-MNL';

INSERT INTO warehouse_staff_assignments (UserId, WarehouseFacilityId, Role, CreatedAt, UpdatedAt)
SELECT u.Id, w.Id, 'ComplianceAdmin', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM users u
INNER JOIN warehouse_facilities w ON w.Code = 'WH-MNL' AND w.IsActive = 1
WHERE u.Email = 'warehouse@agricheck.local'
  AND NOT EXISTS (
    SELECT 1 FROM warehouse_staff_assignments a
    WHERE a.UserId = u.Id AND a.WarehouseFacilityId = w.Id
  );

UPDATE user_profiles p
INNER JOIN users u ON u.Id = p.UserId
SET p.FirstName = 'Manila Cold Storage', p.LastName = 'Operator'
WHERE u.Email = 'warehouse@agricheck.local';

-- Mark EF migration applied (manual SQL path — avoids "table already exists" on dotnet run)
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260928190000_WarehouseStaffLtoApplications', '7.0.20'
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260928190000_WarehouseStaffLtoApplications'
);
