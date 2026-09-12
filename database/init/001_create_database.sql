-- AgriCheck V3 — Initial database setup
-- Run in phpMyAdmin or MySQL CLI (XAMPP)
-- Password: change in production / use .env

CREATE DATABASE IF NOT EXISTS agricheck_v3
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

-- Dev user (adjust password before running)
CREATE USER IF NOT EXISTS 'agricheck_v3'@'localhost' IDENTIFIED BY 'agricheck_v3_dev';
GRANT ALL PRIVILEGES ON agricheck_v3.* TO 'agricheck_v3'@'localhost';
FLUSH PRIVILEGES;

-- Verify
SELECT SCHEMA_NAME, DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME
FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME = 'agricheck_v3';
