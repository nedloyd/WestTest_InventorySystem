using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InventorySystem.Domain.Constants;

namespace InventorySystem.API.Controllers
{
    
    [ApiController]
    [Route("api/products/{productId:int}/batches")]
    [Authorize]
    [Produces("application/json")]
    public class BatchesController(IBatchService batchService) : ControllerBase
    {
        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BatchDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBatches(int productId, CancellationToken ct)
        {
            var batches = await batchService.GetBatchesByProductAsync(productId, ct);
            return Ok(batches);
        }

        
        [HttpPost]
        [Authorize(Roles = Role.WarehouseAdmin)]
        [ProducesResponseType(typeof(BatchDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddBatch(
            int productId,
            [FromBody] AddBatchRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var batch = await batchService.AddBatchAsync(productId, request, ct);
                return StatusCode(StatusCodes.Status201Created, batch);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    }
