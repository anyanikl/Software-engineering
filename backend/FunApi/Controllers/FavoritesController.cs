using FunApi.Interfaces;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _service;

        public FavoritesController(IFavoriteService service)
        {
            _service = service;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<AdvertisementCardDto>>> Get()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetAllAsync(userId.Value));
        }

        [HttpPost("{advertisementId:int}")]
        public async Task<IActionResult> Add(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.AddAsync(userId.Value, advertisementId);
            return NoContent();
        }

        [HttpDelete("{advertisementId:int}")]
        public async Task<IActionResult> Remove(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.RemoveAsync(userId.Value, advertisementId);
            return NoContent();
        }

        [HttpGet("{advertisementId:int}/exists")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<object>> Exists(int advertisementId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(new { exists = await _service.ExistsAsync(userId.Value, advertisementId) });
        }
    }
}
