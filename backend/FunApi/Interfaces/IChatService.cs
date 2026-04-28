using FunDto.Models.Contracts.Chats;

namespace FunApi.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatListItemDto>> GetMyChatsAsync(int userId);
        Task<ChatDto> GetByIdAsync(int userId, int chatId);
        Task<ChatDto> GetOrCreateAsync(int currentUserId, int advertisementId, int? participantUserId = null);
        Task<MessageDto> SendMessageAsync(int senderId, SendMessageDto dto);
        Task MarkAsReadAsync(int userId, int chatId);
    }
}
