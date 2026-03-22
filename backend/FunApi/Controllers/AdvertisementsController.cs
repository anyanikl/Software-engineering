using FunApi.Interfaces;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementsController : ControllerBase
    {
        private readonly IAdvertisementService _service;

        public AdvertisementsController(IAdvertisementService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<object>> List([FromQuery] AdvertisementFilterDto filter)
        {
            var items = await _service.SearchAsync(filter);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<AdvertisementDto>> GetById(int id)
        {
            try { return Ok(await _service.GetByIdAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpGet("my")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<MyAdvertisementDto>>> My([FromQuery] string? status)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetMyAdvertisementsAsync(userId.Value, status));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AdvertisementDto>> Create([FromBody] CreateAdvertisementDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.CreateAsync(userId.Value, dto));
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<AdvertisementDto>> Update(int id, [FromBody] UpdateAdvertisementDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try { return Ok(await _service.UpdateAsync(userId.Value, id, dto)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:int}/archive")]
        [Authorize]
        public async Task<IActionResult> Archive(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try
            {
                await _service.ArchiveAsync(userId.Value, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try
            {
                await _service.DeleteAsync(userId.Value, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}
