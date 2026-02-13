-- Schema_Migration_v2_Phase2.sql
-- Purpose: user security schema hardening and case-insensitive username uniqueness.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- Add security-support columns (app can start using them gradually)
ALTER TABLE Users ADD COLUMN MustChangePassword INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Users ADD COLUMN LastPasswordChangeAt TEXT;
ALTER TABLE Users ADD COLUMN FailedLoginCount INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Users ADD COLUMN LastLoginAt TEXT;

-- Normalize existing usernames (trim spaces)
UPDATE Users SET Username = TRIM(Username);

-- Enforce case-insensitive uniqueness
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NoCase ON Users(Username COLLATE NOCASE);

-- Ensure seeded admin must change password (one-time safety)
UPDATE Users
SET MustChangePassword = 1
WHERE Username = 'admin';

COMMIT;
