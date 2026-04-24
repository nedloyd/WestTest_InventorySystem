-- ============================================================================
-- Seed Data: Sample products and batches for development / testing
-- Run AFTER applying EF Core migrations and deploying the stored procedure.
-- ============================================================================

USE InventoryDB_Dev;
GO

-- ── Products ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.Products (StockKeepingUnitCode, Name, Description, LowStockThreshold, CreatedAt, IsActive)
VALUES
    ('SKU-MED-001', 'Paracetamol 500mg',   'Pain relief tablets, 500mg strength',   100, GETUTCDATE(), 1),
    ('SKU-MED-002', 'Ibuprofen 200mg',     'Anti-inflammatory tablets, 200mg',        50, GETUTCDATE(), 1),
    ('SKU-SUP-001', 'Vitamin C 1000mg',    'Immune support supplement, 1000mg tabs',  30, GETUTCDATE(), 1),
    ('SKU-EQP-001', 'Surgical Gloves L',   'Latex surgical gloves, size large',       20, GETUTCDATE(), 1);
GO

-- ── Batches ───────────────────────────────────────────────────────────────────
-- Product 1 — Paracetamol: 3 batches; first one intentionally low remaining qty
INSERT INTO dbo.Batches (ProductId, BatchNumber, InitialQuantity, RemainingQuantity, ExpiryDate, CreatedAt, IsActive)
VALUES
    -- Oldest batch (will be consumed first by FIFO)
    (1, 'BATCH-PARA-2024-001', 500,  10,  DATEADD(DAY,  30, GETUTCDATE()), DATEADD(DAY, -90, GETUTCDATE()), 1),
    -- Mid batch
    (1, 'BATCH-PARA-2024-002', 500, 200,  DATEADD(DAY, 180, GETUTCDATE()), DATEADD(DAY, -30, GETUTCDATE()), 1),
    -- Newest batch
    (1, 'BATCH-PARA-2024-003', 500, 500,  DATEADD(DAY, 365, GETUTCDATE()), GETUTCDATE(),                    1),

    -- Product 2 — Ibuprofen: 2 batches; one expiring very soon (within 60 days)
    (2, 'BATCH-IBU-2024-001',  300,  40,  DATEADD(DAY,  45, GETUTCDATE()), DATEADD(DAY, -60, GETUTCDATE()), 1),
    (2, 'BATCH-IBU-2024-002',  300, 300,  DATEADD(DAY, 300, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE()), 1),

    -- Product 3 — Vitamin C: 1 batch; below threshold → triggers low-stock alert
    (3, 'BATCH-VITC-2024-001', 200,  25,  DATEADD(DAY, 200, GETUTCDATE()), DATEADD(DAY, -20, GETUTCDATE()), 1),

    -- Product 4 — Surgical Gloves: 1 batch; healthy stock
    (4, 'BATCH-GLVL-2024-001', 100, 100,  DATEADD(DAY, 730, GETUTCDATE()), GETUTCDATE(),                    1);
GO

PRINT 'Seed data inserted successfully.';
