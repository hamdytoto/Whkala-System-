# Schema Enhancement Roadmap

This document now maps directly to executable migration scripts in `Database/`.

## Created Files
- `Database/Schema_Migration_v2_Phase1.sql`
- `Database/Schema_Migration_v2_Phase2.sql`
- `Database/Schema_Migration_v2_Phase3.sql`

## Objectives
- Stronger data integrity
- Better user security
- Faster query performance
- Better auditability
- Safer growth path for accounting/inventory

## Run Order
Apply migrations in this exact order:
1. `Schema_Migration_v2_Phase1.sql`
2. `Schema_Migration_v2_Phase2.sql`
3. `Schema_Migration_v2_Phase3.sql`

## What Each Phase Implements

### Phase 1: Integrity + Performance
- Added critical indexes for joins/filtering on `Sales`, `SaleItems`, `Supplies`, `SupplyItems`, `Payments`, and name search fields.
- Added validation triggers for:
  - role constraints (`Users.Role`)
  - payment type constraints (`Sales.PaymentType`)
  - non-negative financial values
  - valid weights (`GrossWeight >= TareWeight`, `NetWeight >= 0`)
  - positive quantities and amounts

### Phase 2: Security Hardening
- Added user security support columns:
  - `MustChangePassword`
  - `LastPasswordChangeAt`
  - `FailedLoginCount`
  - `LastLoginAt`
- Added case-insensitive username uniqueness:
  - `UX_Users_Username_NoCase`
- Marks seeded admin to require password change.

### Phase 3: Audit + Soft Delete + Ledger Foundation
- Added audit columns to transactional tables:
  - `Sales`, `Supplies`, `FarmerPayments`, `TraderPayments`
- Added `IsActive` soft-delete flags to:
  - `Farmers`, `Traders`, `Products`
- Added optional future-proof ledger tables:
  - `InventoryMovements`
  - `AccountMovements`

## Important Safety Notes
- `ALTER TABLE ... ADD COLUMN` steps in Phase 2/3 are intended to run once on each database.
- Always run on a backup copy first.
- If migration execution is interrupted, restore from backup and rerun cleanly.

## Validation Queries

### Orphans
```sql
SELECT COUNT(*) FROM SaleItems si LEFT JOIN Sales s ON s.Id = si.SaleId WHERE s.Id IS NULL;
SELECT COUNT(*) FROM SupplyItems si LEFT JOIN Supplies s ON s.Id = si.SupplyId WHERE s.Id IS NULL;
```

### Negative-value sanity
```sql
SELECT COUNT(*) FROM Products WHERE CurrentStock < 0;
SELECT COUNT(*) FROM Traders WHERE CreditLimit < 0 OR CurrentBalance < 0;
```

### Username collision (case-insensitive)
```sql
SELECT LOWER(Username), COUNT(*)
FROM Users
GROUP BY LOWER(Username)
HAVING COUNT(*) > 1;
```

## Implementation Checklist
- [x] Create `Schema_Migration_v2_Phase1.sql` with indexes and integrity validations.
- [x] Create `Schema_Migration_v2_Phase2.sql` for security/case-insensitive usernames.
- [x] Create `Schema_Migration_v2_Phase3.sql` for audit/soft-delete/ledger foundations.
- [ ] Update app logic to use hashed passwords instead of plain text.
- [ ] Update login flow to enforce `MustChangePassword`.
- [ ] Update inserts/updates to populate `CreatedByUserId` and `UpdatedAt`.
- [ ] Decide whether to activate ledger-based balances now or later.

## Next Recommended Step
Apply Phase 1 first, test app workflows, then proceed to Phase 2 and 3.
