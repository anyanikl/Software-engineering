using FunApi.Interfaces;
using FunDto.Models.Contracts.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<CartDto>> Get()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetCartAsync(userId.Value));
        }

        [HttpPost("items/{advertisementId:int}")]
        public async Task<IActionResult> Add(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try
            {
                await _service.AddItemAsync(userId.Value, advertisementId);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpDelete("items/{advertisementId:int}")]
        public async Task<IActionResult> Remove(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.RemoveItemAsync(userId.Value, advertisementId);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.ClearAsync(userId.Value);
            return NoContent();
        }
    }
}
