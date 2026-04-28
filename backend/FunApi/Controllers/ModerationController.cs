using FunApi.Interfaces;
using FunApi.Security;
using FunDto.Models.Contracts.Advertisements;
using FunDto.Models.Contracts.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public class ModerationController : ControllerBase
    {
        private readonly IModerationService _service;

        public ModerationController(IModerationService service)
        {
            _service = service;
        }

        [HttpGet("pending")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<ModerationAdvertisementDto>>> Pending()
        {
            var moderatorId = ControllerHelpers.GetCurrentUserId(this);
            if (moderatorId is null) return Unauthorized();
            return Ok(await _service.GetPendingAsync(moderatorId.Value));
        }

        [HttpPost("{advertisementId:int}/approve")]
        public async Task<IActionResult> Approve(int advertisementId, [FromBody] ModerationDecisionDto dto)
        {
            var moderatorId = ControllerHelpers.GetCurrentUserId(this);
            if (moderatorId is null) return Unauthorized();
            await _service.ApproveAsync(moderatorId.Value, advertisementId, dto.Comment);
            return NoContent();
        }

        [HttpPost("{advertisementId:int}/reject")]
        public async Task<IActionResult> Reject(int advertisementId, [FromBody] ModerationDecisionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment)) return BadRequest(new { message = "Comment is required" });
            var moderatorId = ControllerHelpers.GetCurrentUserId(this);
            if (moderatorId is null) return Unauthorized();
            await _service.RejectAsync(moderatorId.Value, advertisementId, dto.Comment);
            return NoContent();
        }

        [HttpPost("{advertisementId:int}/revision")]
        public async Task<IActionResult> Revision(int advertisementId, [FromBody] ModerationDecisionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment)) return BadRequest(new { message = "Comment is required" });
            var moderatorId = ControllerHelpers.GetCurrentUserId(this);
            if (moderatorId is null) return Unauthorized();
            await _service.SendForRevisionAsync(moderatorId.Value, advertisementId, dto.Comment);
            return NoContent();
        }
    }
}
