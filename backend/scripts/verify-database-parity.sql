-- AgriCheck v3 database parity check (local + Hostinger production).
-- Run in phpMyAdmin. Do NOT use information_schema on Hostinger.
--
-- Expected result: 84 rows total
--   83 application tables + __EFMigrationsHistory
--
-- If count is wrong, run:
--   Production: backend/scripts/sync-production-schema.sql (one statement at a time)
--   Local:      backend/scripts/sync-local-schema.sql (one statement at a time)
-- Or restart API so ProductionSchemaSeeder runs (Railway only).

SHOW TABLES;
