-- Schema_Migration_v2_Phase1.sql
-- Purpose: performance + integrity validations without breaking existing app logic.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- ------------------------------------------------------------
-- 1) PERFORMANCE INDEXES
-- ------------------------------------------------------------
CREATE INDEX IF NOT EXISTS IX_Sales_TraderDate ON Sales(TraderId, SaleDate);
CREATE INDEX IF NOT EXISTS IX_SaleItems_SaleId ON SaleItems(SaleId);
CREATE INDEX IF NOT EXISTS IX_SaleItems_FarmerId ON SaleItems(FarmerId);
CREATE INDEX IF NOT EXISTS IX_SaleItems_ProductId ON SaleItems(ProductId);

CREATE INDEX IF NOT EXISTS IX_Supplies_FarmerDate ON Supplies(FarmerId, SupplyDate);
CREATE INDEX IF NOT EXISTS IX_SupplyItems_SupplyId ON SupplyItems(SupplyId);
CREATE INDEX IF NOT EXISTS IX_SupplyItems_ProductId ON SupplyItems(ProductId);

CREATE INDEX IF NOT EXISTS IX_FarmerPayments_FarmerDate ON FarmerPayments(FarmerId, PaymentDate);
CREATE INDEX IF NOT EXISTS IX_TraderPayments_TraderDate ON TraderPayments(TraderId, PaymentDate);

CREATE INDEX IF NOT EXISTS IX_Farmers_Name ON Farmers(Name);
CREATE INDEX IF NOT EXISTS IX_Traders_Name ON Traders(Name);
CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);

-- ------------------------------------------------------------
-- 2) INTEGRITY TRIGGERS (SQLite-safe alternative to ALTER CHECK)
-- ------------------------------------------------------------

-- Users.Role must be Admin/Operator
CREATE TRIGGER IF NOT EXISTS TR_Users_ValidateRole_Insert
BEFORE INSERT ON Users
FOR EACH ROW
WHEN NEW.Role NOT IN ('Admin', 'Operator')
BEGIN
    SELECT RAISE(ABORT, 'Invalid role. Allowed: Admin, Operator');
END;

CREATE TRIGGER IF NOT EXISTS TR_Users_ValidateRole_Update
BEFORE UPDATE OF Role ON Users
FOR EACH ROW
WHEN NEW.Role NOT IN ('Admin', 'Operator')
BEGIN
    SELECT RAISE(ABORT, 'Invalid role. Allowed: Admin, Operator');
END;

-- Sales.PaymentType must be Cash/Credit and total >= 0
CREATE TRIGGER IF NOT EXISTS TR_Sales_Validate_Insert
BEFORE INSERT ON Sales
FOR EACH ROW
WHEN NEW.PaymentType NOT IN ('Cash', 'Credit') OR NEW.TotalAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid Sales row: PaymentType or TotalAmount');
END;

CREATE TRIGGER IF NOT EXISTS TR_Sales_Validate_Update
BEFORE UPDATE OF PaymentType, TotalAmount ON Sales
FOR EACH ROW
WHEN NEW.PaymentType NOT IN ('Cash', 'Credit') OR NEW.TotalAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid Sales row: PaymentType or TotalAmount');
END;

-- Products.CurrentStock >= 0
CREATE TRIGGER IF NOT EXISTS TR_Products_Validate_Insert
BEFORE INSERT ON Products
FOR EACH ROW
WHEN NEW.CurrentStock < 0
BEGIN
    SELECT RAISE(ABORT, 'CurrentStock cannot be negative');
END;

CREATE TRIGGER IF NOT EXISTS TR_Products_Validate_Update
BEFORE UPDATE OF CurrentStock ON Products
FOR EACH ROW
WHEN NEW.CurrentStock < 0
BEGIN
    SELECT RAISE(ABORT, 'CurrentStock cannot be negative');
END;

-- Traders financial values non-negative
CREATE TRIGGER IF NOT EXISTS TR_Traders_Validate_Insert
BEFORE INSERT ON Traders
FOR EACH ROW
WHEN NEW.CreditLimit < 0 OR NEW.CurrentBalance < 0
BEGIN
    SELECT RAISE(ABORT, 'CreditLimit and CurrentBalance cannot be negative');
END;

CREATE TRIGGER IF NOT EXISTS TR_Traders_Validate_Update
BEFORE UPDATE OF CreditLimit, CurrentBalance ON Traders
FOR EACH ROW
WHEN NEW.CreditLimit < 0 OR NEW.CurrentBalance < 0
BEGIN
    SELECT RAISE(ABORT, 'CreditLimit and CurrentBalance cannot be negative');
END;

-- Payments.Amount > 0
CREATE TRIGGER IF NOT EXISTS TR_FarmerPayments_Validate_Insert
BEFORE INSERT ON FarmerPayments
FOR EACH ROW
WHEN NEW.Amount <= 0
BEGIN
    SELECT RAISE(ABORT, 'FarmerPayments.Amount must be > 0');
END;

CREATE TRIGGER IF NOT EXISTS TR_FarmerPayments_Validate_Update
BEFORE UPDATE OF Amount ON FarmerPayments
FOR EACH ROW
WHEN NEW.Amount <= 0
BEGIN
    SELECT RAISE(ABORT, 'FarmerPayments.Amount must be > 0');
END;

CREATE TRIGGER IF NOT EXISTS TR_TraderPayments_Validate_Insert
BEFORE INSERT ON TraderPayments
FOR EACH ROW
WHEN NEW.Amount <= 0
BEGIN
    SELECT RAISE(ABORT, 'TraderPayments.Amount must be > 0');
END;

CREATE TRIGGER IF NOT EXISTS TR_TraderPayments_Validate_Update
BEFORE UPDATE OF Amount ON TraderPayments
FOR EACH ROW
WHEN NEW.Amount <= 0
BEGIN
    SELECT RAISE(ABORT, 'TraderPayments.Amount must be > 0');
END;

-- SupplyItems checks
CREATE TRIGGER IF NOT EXISTS TR_SupplyItems_Validate_Insert
BEFORE INSERT ON SupplyItems
FOR EACH ROW
WHEN NEW.Quantity <= 0
  OR NEW.GrossWeight < NEW.TareWeight
  OR NEW.NetWeight < 0
  OR NEW.ExpectedPricePerUnit < 0
  OR NEW.TotalPrice < 0
  OR NEW.CommissionAmount < 0
  OR NEW.FarmerNetAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid SupplyItems values');
END;

CREATE TRIGGER IF NOT EXISTS TR_SupplyItems_Validate_Update
BEFORE UPDATE ON SupplyItems
FOR EACH ROW
WHEN NEW.Quantity <= 0
  OR NEW.GrossWeight < NEW.TareWeight
  OR NEW.NetWeight < 0
  OR NEW.ExpectedPricePerUnit < 0
  OR NEW.TotalPrice < 0
  OR NEW.CommissionAmount < 0
  OR NEW.FarmerNetAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid SupplyItems values');
END;

-- SaleItems checks
CREATE TRIGGER IF NOT EXISTS TR_SaleItems_Validate_Insert
BEFORE INSERT ON SaleItems
FOR EACH ROW
WHEN NEW.Quantity <= 0
  OR NEW.GrossWeight < NEW.TareWeight
  OR NEW.NetWeight < 0
  OR NEW.PricePerUnit < 0
  OR NEW.TotalAmount < 0
  OR NEW.CommissionAmount < 0
  OR NEW.FarmerNetAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid SaleItems values');
END;

CREATE TRIGGER IF NOT EXISTS TR_SaleItems_Validate_Update
BEFORE UPDATE ON SaleItems
FOR EACH ROW
WHEN NEW.Quantity <= 0
  OR NEW.GrossWeight < NEW.TareWeight
  OR NEW.NetWeight < 0
  OR NEW.PricePerUnit < 0
  OR NEW.TotalAmount < 0
  OR NEW.CommissionAmount < 0
  OR NEW.FarmerNetAmount < 0
BEGIN
    SELECT RAISE(ABORT, 'Invalid SaleItems values');
END;

COMMIT;
