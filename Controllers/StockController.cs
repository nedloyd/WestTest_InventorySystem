using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers
{
   
    [ApiController]
    [Route("api/stock")]
    [Authorize]
    [Produces("application/json")]
    public class StockController(IBatchService batchService) : ControllerBase
    {
        [HttpPost("consume")]
        [Authorize(Roles = Role.WarehouseAdmin)]
        [ProducesResponseType(typeof(ConsumeStockResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ConsumeStock(
            [FromBody] ConsumeStockRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            
            var consumedBy = User.Identity?.Name ?? "system";
            var result = await batchService.ConsumeStockAsync(request, consumedBy, ct);

            return Ok(result);
        }

    }

}
