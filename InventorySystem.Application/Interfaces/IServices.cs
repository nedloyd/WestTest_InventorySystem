using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces
{

    
    public interface IProductService
    {
    
        Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken ct = default);

        Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken ct = default);

        Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    }

    public interface IBatchService
    {

        Task<IEnumerable<BatchDto>> GetBatchesByProductAsync(int productId, CancellationToken ct = default);
        Task<BatchDto> AddBatchAsync(int productId, AddBatchRequest request, CancellationToken ct = default);
        Task<ConsumeStockResult> ConsumeStockAsync(ConsumeStockRequest request, string consumedBy, CancellationToken ct = default);
    }

    public interface IInventoryAnalyticsService
    {
        Task<IEnumerable<LowStockAlert>> GetLowStockAlertsAsync(CancellationToken ct = default);

        Task<IEnumerable<ExpiringBatchAlert>> GetExpiringBatchesAsync(CancellationToken ct = default);
    }

    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }


    public interface IFifoStockRepository
    {
        Task<List<BatchConsumedDetail>> ConsumeStockFifoAsync(
            int productId,
            int quantityToConsume,
            string reason,
            string consumedBy,
            CancellationToken ct = default);
    }

}
