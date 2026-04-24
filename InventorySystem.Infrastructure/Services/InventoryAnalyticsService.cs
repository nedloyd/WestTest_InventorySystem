using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem.Infrastructure.Services
{
    public class InventoryAnalyticsService(InventoryDbContext db) :IInventoryAnalyticsService
    {
      
        public async Task<IEnumerable<LowStockAlert>> GetLowStockAlertsAsync(CancellationToken ct = default)
        {
            var alerts = await db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.StockKeepingUnitCode,
                    p.Name,
                    p.LowStockThreshold,
                    TotalStock = p.Batches
                        .Where(b => b.IsActive && b.RemainingQuantity > 0)
                        .Sum(b => (int?)b.RemainingQuantity) ?? 0
                })
                .Where(x => x.TotalStock <= x.LowStockThreshold)
                .OrderBy(x => x.TotalStock)   
                .ToListAsync(ct);

            return alerts.Select(a => new LowStockAlert(
                a.Id, a.StockKeepingUnitCode, a.Name, a.TotalStock, a.LowStockThreshold));
        }

        
        public async Task<IEnumerable<ExpiringBatchAlert>> GetExpiringBatchesAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(60);  //to be read from configuration

            var batches = await db.Batches
                .AsNoTracking()
                .Where(b => b.IsActive
                         && b.RemainingQuantity > 0
                         && b.ExpiryDate >= now           // Not yet expired
                         && b.ExpiryDate <= threshold)    // Expiring within window
                .Include(b => b.Product)                  
                .OrderBy(b => b.ExpiryDate)               // Soonest-expiring first
                .ToListAsync(ct);

            return batches.Select(b => new ExpiringBatchAlert(
                BatchId: b.Id,
                BatchNumber: b.BatchNumber,
                ProductId: b.ProductId,
                ProductName: b.Product?.Name ?? "Unknown",
                RemainingQuantity: b.RemainingQuantity,
                ExpiryDate: b.ExpiryDate,
                DaysUntilExpiry: (int)(b.ExpiryDate - now).TotalDays));
        }

    }
}
