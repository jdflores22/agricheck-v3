-- Idempotent backfill: DA warehouse LTO + importer facility affiliations (local/staging).
-- Requires: 20260928120000_WarehouseLtoImporterFacilities (warehouse_operating_licenses, importer_facility_affiliations).

SET @issued_by := (
    SELECT u.Id FROM users u
    INNER JOIN user_roles ur ON ur.UserId = u.Id
    INNER JOIN roles r ON r.Id = ur.RoleId AND r.Code = 'ROLE_ADMIN'
    LIMIT 1
);
SET @issued_by := IFNULL(@issued_by, (SELECT Id FROM users ORDER BY Id LIMIT 1));

INSERT INTO warehouse_operating_licenses (
    WarehouseFacilityId, LicenseNumber, Status, IssuedAt, ExpiresAt, IssuedByUserId, Notes, CreatedAt, UpdatedAt
)
SELECT
    w.Id,
    CONCAT('DA-LTO-', w.Code),
    'Active',
    UTC_TIMESTAMP() - INTERVAL 1 YEAR,
    UTC_TIMESTAMP() + INTERVAL 5 YEAR,
    @issued_by,
    'SQL backfill — replace with real DA LTO in production',
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM warehouse_facilities w
WHERE w.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM warehouse_operating_licenses l
      WHERE l.WarehouseFacilityId = w.Id
        AND l.Status = 'Active'
        AND l.ExpiresAt > UTC_TIMESTAMP()
  );

INSERT INTO importer_facility_affiliations (Uuid, UserId, WarehouseFacilityId, CreatedAt, UpdatedAt)
SELECT UUID(), e.UserId, e.FinalWarehouseFacilityId, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM entries e
WHERE e.FinalWarehouseFacilityId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM importer_facility_affiliations a
      WHERE a.UserId = e.UserId AND a.WarehouseFacilityId = e.FinalWarehouseFacilityId
  );

INSERT INTO importer_facility_affiliations (Uuid, UserId, WarehouseFacilityId, CreatedAt, UpdatedAt)
SELECT UUID(), r.SubmittedByUserId, r.WarehouseFacilityId, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM facility_registrations r
WHERE NOT EXISTS (
    SELECT 1 FROM importer_facility_affiliations a
    WHERE a.UserId = r.SubmittedByUserId AND a.WarehouseFacilityId = r.WarehouseFacilityId
);

-- Demo/local: link each importer/exporter/broker with no affiliation yet to one active LTO warehouse.
INSERT INTO importer_facility_affiliations (Uuid, UserId, WarehouseFacilityId, CreatedAt, UpdatedAt)
SELECT UUID(), u.Id, wh.Id, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM users u
INNER JOIN user_roles ur ON ur.UserId = u.Id
INNER JOIN roles ro ON ro.Id = ur.RoleId AND ro.Code IN ('ROLE_IMPORTER', 'ROLE_EXPORTER', 'ROLE_BROKER')
CROSS JOIN (
    SELECT w.Id
    FROM warehouse_facilities w
    INNER JOIN warehouse_operating_licenses l ON l.WarehouseFacilityId = w.Id
    WHERE w.IsActive = 1 AND l.Status = 'Active' AND l.ExpiresAt > UTC_TIMESTAMP()
    ORDER BY w.Id
    LIMIT 1
) wh
WHERE NOT EXISTS (SELECT 1 FROM importer_facility_affiliations a WHERE a.UserId = u.Id);
