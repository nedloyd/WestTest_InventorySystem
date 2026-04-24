USE [InventorySystem]
GO

/****** Object:  StoredProcedure [dbo].[sp_ConsumeStockFIFO]    Script Date: 23-04-2026 13:12:27 ******/
DROP PROCEDURE [dbo].[sp_ConsumeStockFIFO]
GO

/****** Object:  StoredProcedure [dbo].[sp_ConsumeStockFIFO]    Script Date: 23-04-2026 13:12:27 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[sp_ConsumeStockFIFO]
    @ProductId         INT,
    @QuantityToConsume INT,
    @Reason            NVARCHAR(500),
    @ConsumedBy        NVARCHAR(100),
    @ErrorMessage      NVARCHAR(500) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;   -- Auto-rollback on any error

    -- ── Input validation ────────────────────────────────────────────────────
    IF @QuantityToConsume <= 0
    BEGIN
        SET @ErrorMessage = 'Quantity to consume must be greater than zero.';
        RETURN;
    END

    -- ── Temp table to collect the per-batch breakdown for the result set ────
    CREATE TABLE #ConsumedBatches
    (
        BatchId          INT           NOT NULL,
        BatchNumber      NVARCHAR(100) NOT NULL,
        QuantityConsumed INT           NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Remaining       INT = @QuantityToConsume;
        DECLARE @BatchId         INT;
        DECLARE @BatchNumber     NVARCHAR(100);
        DECLARE @BatchRemaining  INT;
        DECLARE @ConsumeFromBatch INT;

        -- ── FIFO cursor: oldest batches first ─────────────────────────────
        -- UPDLOCK prevents phantom reads; ROWLOCK keeps contention minimal.
        DECLARE fifo_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT   Id, BatchNumber, RemainingQuantity
            FROM     dbo.Batches WITH (UPDLOCK, ROWLOCK)
            WHERE    ProductId         = @ProductId
              AND    IsActive          = 1
              AND    RemainingQuantity > 0
            ORDER BY CreatedAt ASC;   -- ← FIFO order

        OPEN fifo_cursor;
        FETCH NEXT FROM fifo_cursor INTO @BatchId, @BatchNumber, @BatchRemaining;

        WHILE @@FETCH_STATUS = 0 AND @Remaining > 0
        BEGIN
            -- How much can we take from this batch?
            SET @ConsumeFromBatch = CASE
                WHEN @BatchRemaining >= @Remaining THEN @Remaining
                ELSE @BatchRemaining
            END;

            -- ── Deduct from batch ────────────────────────────────────────
            UPDATE dbo.Batches
            SET    RemainingQuantity = RemainingQuantity - @ConsumeFromBatch,
                   -- Auto-deactivate if fully consumed
                   IsActive          = CASE
                                           WHEN RemainingQuantity - @ConsumeFromBatch = 0
                                           THEN 0
                                           ELSE 1
                                       END
            WHERE  Id = @BatchId;

            -- ── Insert audit record ──────────────────────────────────────
            INSERT INTO dbo.StockMovements
                (ProductId, BatchId, QuantityChanged, Reason, CreatedBy, CreatedAt)
            VALUES
                (@ProductId, @BatchId, -@ConsumeFromBatch, @Reason, @ConsumedBy, GETUTCDATE());

            -- Collect for result set
            INSERT INTO #ConsumedBatches (BatchId, BatchNumber, QuantityConsumed)
            VALUES (@BatchId, @BatchNumber, @ConsumeFromBatch);

            SET @Remaining = @Remaining - @ConsumeFromBatch;

            FETCH NEXT FROM fifo_cursor INTO @BatchId, @BatchNumber, @BatchRemaining;
        END

        CLOSE fifo_cursor;
        DEALLOCATE fifo_cursor;

        -- ── Did we satisfy the full quantity? ────────────────────────────
        IF @Remaining > 0
        BEGIN
            SET @ErrorMessage = CONCAT(
                'Insufficient stock. Could not fulfil remaining ',
                @Remaining, ' unit(s).');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        COMMIT TRANSACTION;

        -- Return per-batch breakdown to the caller (ADO.NET reads this)
        SELECT BatchId, BatchNumber, QuantityConsumed
        FROM   #ConsumedBatches;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorMessage = ERROR_MESSAGE();

        -- Re-raise so the ADO.NET caller also sees the exception
        THROW;
    END CATCH

    DROP TABLE IF EXISTS #ConsumedBatches;
END;
GO


