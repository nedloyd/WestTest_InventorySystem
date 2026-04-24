using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Domain.Entities;


namespace InventorySystem.Infrastructure.Services
{
   
    public class BatchService(
    InventoryDbContext db,
    IFifoStockRepository fifoRepo,
    ILogger<BatchService> logger) : IBatchService
    {
        public async Task<IEnumerable<BatchDto>> GetBatchesByProductAsync(int productId, CancellationToken ct = default)
        {
            var expiryThreshold = DateTime.UtcNow.AddDays(60); //to be read from configuration

            return await db.Batches
                .AsNoTracking()
                .Where(b => b.ProductId == productId && b.IsActive)
                .OrderBy(b => b.CreatedAt)   // FIFO order for display
                .Select(b => new BatchDto(
                b.Id,
                b.BatchNumber,
                b.InitialQuantity,
                b.RemainingQuantity,
                b.ExpiryDate,
                b.ExpiryDate <= expiryThreshold))
            .ToListAsync(ct);
        }

        public async Task<BatchDto> AddBatchAsync(int productId, AddBatchRequest request, CancellationToken ct = default)
        {
            bool productExists = await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct);
            if (!productExists)
                throw new KeyNotFoundException($"Product with Id={productId} not found.");

            bool batchExists = await db.Batches
                .AnyAsync(b => b.ProductId == productId && b.BatchNumber == request.BatchNumber, ct);
            if (batchExists)
                throw new InvalidOperationException(
                    $"Batch '{request.BatchNumber}' already exists for product {productId}.");

            var batch = new Batch
            {
                ProductId = productId,
                BatchNumber = request.BatchNumber.Trim().ToUpperInvariant(),
                InitialQuantity = request.Quantity,
                RemainingQuantity = request.Quantity,
                ExpiryDate = request.ExpiryDate.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow
            };

            db.Batches.Add(batch);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Added batch {BatchNumber} (Id={Id}) to product {ProductId}, qty={Qty}",
                batch.BatchNumber, batch.Id, productId, request.Quantity);

            var expiryThreshold = DateTime.UtcNow.AddDays(60);

            return new BatchDto(
                batch.Id, batch.BatchNumber,
                batch.InitialQuantity, batch.RemainingQuantity,
                batch.ExpiryDate,
                IsExpiringSoon: batch.ExpiryDate <= expiryThreshold);
        }

       
        public async Task<ConsumeStockResult> ConsumeStockAsync(
            ConsumeStockRequest request,
            string consumedBy,
            CancellationToken ct = default)
        {
            int totalAvailable = await db.Batches
                .Where(b => b.ProductId == request.ProductId && b.IsActive && b.RemainingQuantity > 0)
                .SumAsync(b => b.RemainingQuantity, ct);

            if (totalAvailable < request.Quantity)
            {
                return new ConsumeStockResult(
                    Success: false,
                    Message: $"Insufficient stock. Requested={request.Quantity}, Available={totalAvailable}.",
                    ConsumedFrom: []);
            }

            try
            {
                var details = await fifoRepo.ConsumeStockFifoAsync(
                    request.ProductId,
                    request.Quantity,
                    request.Reason,
                    consumedBy,
                    ct);

                logger.LogInformation(
                    "Stock consumed — ProductId={ProductId}, Qty={Qty}, By={User}",
                    request.ProductId, request.Quantity, consumedBy);

                return new ConsumeStockResult(
                    Success: true,
                    Message: $"Successfully consumed {request.Quantity} unit(s).",
                    ConsumedFrom: details);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "FIFO consumption failed for ProductId={ProductId}", request.ProductId);
                return new ConsumeStockResult(Success: false, Message: ex.Message, ConsumedFrom: []);
            }
        }

    }
}
