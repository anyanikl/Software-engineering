using FunApi.Interfaces;
using FunDto.Models.Contracts.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;

        public AdminController(IAdminService service)
        {
            _service = service;
        }

        [HttpGet("users")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<AdminStatsDto>>> Users([FromQuery] UserAdminFilterDto filter)
        {
            return Ok(await _service.GetUsersAsync(filter));
        }

        [HttpPost("users/{userId:int}/block")]
        public async Task<IActionResult> Block(int userId)
        {
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            await _service.BlockUserAsync(adminId.Value, userId);
            return NoContent();
        }

        [HttpPost("users/{userId:int}/unblock")]
        public async Task<IActionResult> Unblock(int userId)
        {
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            await _service.UnblockUserAsync(adminId.Value, userId);
            return NoContent();
        }

        [HttpGet("stats")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<UserAdminDto>> Stats()
        {
            return Ok(await _service.GetStatsAsync());
        }

        [HttpGet("export/csv")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<string>> ExportCsv()
        {
            return Ok(await _service.ExportUsersCsvAsync());
        }

        [HttpGet("export/json")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<string>> ExportJson()
        {
            return Ok(await _service.ExportUsersJsonAsync());
        }
    }
}
