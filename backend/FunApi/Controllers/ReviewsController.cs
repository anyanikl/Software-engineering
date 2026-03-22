using FunApi.Interfaces;
using FunDto.Models.Contracts.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service)
        {
            _service = service;
        }

        [HttpGet("users/{userId:int}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<ReviewDto>>> ByUser(int userId)
        {
            return Ok(await _service.GetByUserIdAsync(userId));
        }

        [HttpGet("can-leave")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<object>> CanLeave([FromQuery] int orderId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(new { canLeave = await _service.CanLeaveReviewAsync(userId.Value, orderId) });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.CreateAsync(userId.Value, dto));
        }
    }
}
