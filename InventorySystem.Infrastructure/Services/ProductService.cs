using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Domain.Entities;
using InventorySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem.Infrastructure.Services
{
   
    public class ProductService(InventoryDbContext db,ILogger<ProductService> logger):IProductService
    {
        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken ct = default)
        {
           
            var products = await db.Products
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
                .ToListAsync(ct);

            
            return products.Select(p => new ProductDto(
                p.Id, p.StockKeepingUnitCode, p.Name, p.LowStockThreshold,
                p.TotalStock,
                IsLowStock: p.TotalStock <= p.LowStockThreshold));
        }

        
        public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken ct = default)
        {
            var p = await db.Products
                .AsNoTracking()
                .Where(x => x.Id == id && x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.StockKeepingUnitCode,
                    x.Name,
                    x.LowStockThreshold,
                    TotalStock = x.Batches
                        .Where(b => b.IsActive && b.RemainingQuantity > 0)
                        .Sum(b => (int?)b.RemainingQuantity) ?? 0
                })
                .FirstOrDefaultAsync(ct);

            if (p is null) return null;

            return new ProductDto(
                p.Id, p.StockKeepingUnitCode, p.Name, p.LowStockThreshold,
                p.TotalStock,
                IsLowStock: p.TotalStock <= p.LowStockThreshold);
        }

        
        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
        {
        
            bool skuExists = await db.Products
                .AnyAsync(p => p.StockKeepingUnitCode == request.StockKeepingUnitCode && p.IsActive, ct);

            if (skuExists)
                throw new InvalidOperationException($"A product with SKU '{request.StockKeepingUnitCode}' already exists.");

            var product = new Product
            {
                StockKeepingUnitCode = request.StockKeepingUnitCode.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                LowStockThreshold = request.LowStockThreshold,
                CreatedAt = DateTime.UtcNow
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(ct);  

            logger.LogInformation("Created product SKU={SKU}, Id={Id}", product.StockKeepingUnitCode, product.Id);

            return new ProductDto(
                product.Id, product.StockKeepingUnitCode, product.Name,
                product.LowStockThreshold, TotalStock: 0, IsLowStock: true);
        }

    }
}
