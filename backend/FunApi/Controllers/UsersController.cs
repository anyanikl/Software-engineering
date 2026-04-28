using FunApi.Exceptions;
using FunApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FunDto.Models.Contracts.Users;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            try
            {
                return Ok(await _userService.GetMyProfileAsync(userId.Value));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            try
            {
                return Ok(await _userService.UpdateProfileAsync(userId.Value, dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{userId:int}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<PublicUserProfileDto>> GetPublicProfile(int userId)
        {
            try
            {
                return Ok(await _userService.GetPublicProfileAsync(userId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("me/avatar")]
        [Authorize]
        [RequestSizeLimit(5_000_000)]
        public async Task<ActionResult<object>> UploadAvatar([FromForm] IFormFile? avatar)
        {
            if (avatar is null)
            {
                return BadRequest(new { message = "Avatar file is required" });
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            try
            {
                var avatarUrl = await _userService.UpdateAvatarAsync(userId.Value, avatar);
                return Ok(new { avatarUrl });
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("me/avatar")]
        [Authorize]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            await _userService.DeleteAvatarAsync(userId.Value);
            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
