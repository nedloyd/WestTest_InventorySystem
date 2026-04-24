using InventorySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace InventorySystem.Infrastructure.Persistence
{
    public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) :DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.StockKeepingUnitCode).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);

                entity.HasIndex(p => p.StockKeepingUnitCode).IsUnique();
                entity.HasIndex(p => p.IsActive);
            });

            modelBuilder.Entity<Batch>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.BatchNumber).IsRequired().HasMaxLength(100);

                // FK to Product with cascade delete (remove product → remove its batches)
                entity.HasOne(b => b.Product)
                      .WithMany(p => p.Batches)
                      .HasForeignKey(b => b.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FIFO order: queries sort by CreatedAt ASC — this index makes that O(log n)
                entity.HasIndex(b => new { b.ProductId, b.CreatedAt });


                // Expiry-date lookups for the "expiring soon" alert
                entity.HasIndex(b => b.ExpiryDate);

                // Composite index for FIFO consumption: filter active+product, sort by date
                entity.HasIndex(b => new { b.ProductId, b.IsActive, b.RemainingQuantity });
                      
            });

            // ── StockMovement ────────────────────────────────────────────────────
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Reason).IsRequired().HasMaxLength(500);
                entity.Property(s => s.CreatedBy).IsRequired().HasMaxLength(100);

                entity.HasOne(s => s.Product)
                      .WithMany()
                      .HasForeignKey(s => s.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);  // Keep history even if product deleted

                entity.HasOne(s => s.Batch)
                      .WithMany()
                      .HasForeignKey(s => s.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index for audit trail queries (filter by product, order by time)
                entity.HasIndex(s => new { s.ProductId, s.CreatedAt });
                      
            });
        }

    }
}
