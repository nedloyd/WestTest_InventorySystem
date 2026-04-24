using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem.Application.DTOs
{

    
    public record LoginRequest(string Username, string Password);

    public record LoginResponse(string Token, string Role, DateTime ExpiresAt);



    public record CreateProductRequest(
        string StockKeepingUnitCode,
        string Name,
        string? Description,
        int LowStockThreshold);

    public record ProductDto(
        int Id,
        string StockKeepingUnitCode,
        string Name,
        int LowStockThreshold,
        int TotalStock,       
        bool IsLowStock);     // true when TotalStock <= LowStockThreshold

    
    public record AddBatchRequest(
        string BatchNumber,
        DateTime ExpiryDate,
        int Quantity);

    
    public record BatchDto(
        int Id,
        string BatchNumber,
        int InitialQuantity,
        int RemainingQuantity,
        DateTime ExpiryDate,
        bool IsExpiringSoon);  // true when ExpiryDate is within 60 days

   
    public record ConsumeStockRequest(
        int ProductId,
        int Quantity,
        string Reason);

   
    public record ConsumeStockResult(
        bool Success,
        string Message,
        List<BatchConsumedDetail> ConsumedFrom);

   
    public record BatchConsumedDetail(
        int BatchId,
        string BatchNumber,
        int QuantityConsumed);

   
    public record LowStockAlert(
        int ProductId,
        string StockKeepingUnitCode,
        string ProductName,
        int TotalStock,
        int LowStockThreshold);

   
    public record ExpiringBatchAlert(
        int BatchId,
        string BatchNumber,
        int ProductId,
        string ProductName,
        int RemainingQuantity,
        DateTime ExpiryDate,
        int DaysUntilExpiry);

}
