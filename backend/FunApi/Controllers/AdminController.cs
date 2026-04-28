using FunApi.Interfaces;
using FunApi.Security;
using FunDto.Models.Contracts.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
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
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            return Ok(await _service.GetUsersAsync(adminId.Value, filter));
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
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            return Ok(await _service.GetStatsAsync(adminId.Value));
        }

        [HttpGet("export/csv")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<string>> ExportCsv()
        {
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            return Ok(await _service.ExportUsersCsvAsync(adminId.Value));
        }

        [HttpGet("export/json")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<string>> ExportJson()
        {
            var adminId = ControllerHelpers.GetCurrentUserId(this);
            if (adminId is null) return Unauthorized();
            return Ok(await _service.ExportUsersJsonAsync(adminId.Value));
        }
    }
}
