using FunApi.Interfaces;
using FunDto.Models.Contracts.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _service;

        public ChatsController(IChatService service)
        {
            _service = service;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<List<ChatListItemDto>>> Get()
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetMyChatsAsync(userId.Value));
        }

        [HttpGet("{id:int}")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<ChatDto>> GetById(int id)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            try { return Ok(await _service.GetByIdAsync(userId.Value, id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("or-create/{advertisementId:int}")]
        public async Task<ActionResult<ChatDto>> OrCreate(int advertisementId, [FromQuery] int? participantUserId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.GetOrCreateAsync(userId.Value, advertisementId, participantUserId));
        }

        [HttpPost("messages")]
        public async Task<ActionResult<MessageDto>> Send([FromBody] SendMessageDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            return Ok(await _service.SendMessageAsync(userId.Value, dto));
        }

        [HttpPost("{chatId:int}/read")]
        public async Task<IActionResult> Read(int chatId)
        {
            var userId = ControllerHelpers.GetCurrentUserId(this);
            if (userId is null) return Unauthorized();
            await _service.MarkAsReadAsync(userId.Value, chatId);
            return NoContent();
        }
    }
}
