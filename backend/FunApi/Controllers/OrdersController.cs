using FunApi.Interfaces;
using FunDto.Models.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        [HttpPost("from-cart")]
        public async Task<ActionResult<OrderDto>> FromCart()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.CreateFromCartAsync(userId.Value));
        }

        [HttpPost("single/{advertisementId:int}")]
        public async Task<ActionResult<OrderDto>> Single(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.CreateSingleAsync(userId.Value, advertisementId));
        }

        [HttpGet("buyer")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<OrderDto>>> Buyer()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetBuyerOrdersAsync(userId.Value));
        }

        [HttpGet("seller")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<OrderDto>>> Seller()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetSellerOrdersAsync(userId.Value));
        }

        [HttpGet("{id:int}")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<OrderDto>> GetById(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try { return Ok(await _service.GetByIdAsync(userId.Value, id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.CompleteAsync(userId.Value, id);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.CancelAsync(userId.Value, id);
            return NoContent();
        }
    }
}
