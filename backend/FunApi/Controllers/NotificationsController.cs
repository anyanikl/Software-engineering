using FunApi.Interfaces;
using FunDto.Models.Contracts.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<NotificationDto>>> Get()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetMyNotificationsAsync(userId.Value));
        }

        [HttpGet("unread-count")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<object>> UnreadCount()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(new { unreadCount = await _service.GetUnreadCountAsync(userId.Value) });
        }

        [HttpPost("{notificationId:int}/read")]
        public async Task<IActionResult> Read(int notificationId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.MarkAsReadAsync(userId.Value, notificationId);
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> ReadAll()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.MarkAllAsReadAsync(userId.Value);
            return NoContent();
        }
    }
}
