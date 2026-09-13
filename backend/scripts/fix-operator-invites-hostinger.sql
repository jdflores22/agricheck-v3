-- Run this once in Hostinger phpMyAdmin on the production database.
-- Fixes missing operator_invite_codes table and seeds AGRITRACK-DEMO.

CREATE TABLE IF NOT EXISTS `operator_invite_codes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Code` varchar(32) NOT NULL,
  `OperatorUserId` bigint NOT NULL,
  `Label` varchar(128) NULL,
  `MaxUses` int NOT NULL DEFAULT 0,
  `UsedCount` int NOT NULL DEFAULT 0,
  `ExpiresAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_operator_invite_codes_Code` (`Code`),
  KEY `IX_operator_invite_codes_OperatorUserId` (`OperatorUserId`),
  CONSTRAINT `FK_operator_invite_codes_users_OperatorUserId`
    FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `operator_invite_codes`
  (`Code`, `OperatorUserId`, `Label`, `MaxUses`, `UsedCount`, `IsActive`, `CreatedAt`, `UpdatedAt`)
SELECT
  'AGRITRACK-DEMO',
  u.`Id`,
  'AgriTrack demo fleet',
  0,
  0,
  1,
  UTC_TIMESTAMP(6),
  UTC_TIMESTAMP(6)
FROM `users` u
WHERE u.`Email` = 'operator@agricheck.local'
  AND NOT EXISTS (
    SELECT 1 FROM `operator_invite_codes` ic WHERE ic.`OperatorUserId` = u.`Id`
  );
