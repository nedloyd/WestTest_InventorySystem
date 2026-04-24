using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers
{
    
    [ApiController]
    [Route("api/analytics")]
    [Authorize]  
    [Produces("application/json")]
    public class AnalyticsController(IInventoryAnalyticsService analyticsService) : ControllerBase
    { 
        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(IEnumerable<LowStockAlert>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLowStockAlerts(CancellationToken ct)
        {
            var alerts = await analyticsService.GetLowStockAlertsAsync(ct);
            return Ok(alerts);
        }

      
        [HttpGet("expiring-batches")]
        [ProducesResponseType(typeof(IEnumerable<ExpiringBatchAlert>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExpiringBatches(CancellationToken ct)
        {
            var alerts = await analyticsService.GetExpiringBatchesAsync(ct);
            return Ok(alerts);
        }

    }
}
