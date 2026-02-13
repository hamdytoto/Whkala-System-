-- Schema_Migration_v2_Phase3.sql
-- Purpose: auditability + soft delete + ledger foundation.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- ------------------------------------------------------------
-- 1) Audit Columns
-- ------------------------------------------------------------
ALTER TABLE Sales ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));
ALTER TABLE Sales ADD COLUMN UpdatedAt TEXT;
ALTER TABLE Sales ADD COLUMN CreatedByUserId INTEGER;

ALTER TABLE Supplies ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));
ALTER TABLE Supplies ADD COLUMN UpdatedAt TEXT;
ALTER TABLE Supplies ADD COLUMN CreatedByUserId INTEGER;

ALTER TABLE FarmerPayments ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));
ALTER TABLE FarmerPayments ADD COLUMN CreatedByUserId INTEGER;

ALTER TABLE TraderPayments ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));
ALTER TABLE TraderPayments ADD COLUMN CreatedByUserId INTEGER;

-- ------------------------------------------------------------
-- 2) Soft Delete (Master Data)
-- ------------------------------------------------------------
ALTER TABLE Farmers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;
ALTER TABLE Traders ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;
ALTER TABLE Products ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;

-- ------------------------------------------------------------
-- 3) Optional Ledger Foundation
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS InventoryMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    MovementDate TEXT NOT NULL DEFAULT (datetime('now')),
    Direction TEXT NOT NULL, -- IN / OUT
    Quantity REAL NOT NULL,
    SourceType TEXT NOT NULL, -- Supply / Sale / Adjustment
    SourceId INTEGER,
    Notes TEXT,
    CreatedByUserId INTEGER,
    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS AccountMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PartyType TEXT NOT NULL, -- Farmer / Trader
    PartyId INTEGER NOT NULL,
    MovementDate TEXT NOT NULL DEFAULT (datetime('now')),
    Direction TEXT NOT NULL, -- Debit / Credit
    Amount REAL NOT NULL,
    SourceType TEXT NOT NULL, -- Sale / Supply / Payment / Adjustment
    SourceId INTEGER,
    Notes TEXT,
    CreatedByUserId INTEGER,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

CREATE INDEX IF NOT EXISTS IX_InventoryMovements_ProductDate ON InventoryMovements(ProductId, MovementDate);
CREATE INDEX IF NOT EXISTS IX_AccountMovements_PartyDate ON AccountMovements(PartyType, PartyId, MovementDate);

COMMIT;
