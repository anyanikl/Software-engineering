namespace FunDto.Models.Contracts.Chats
{
    public class ChatListItemDto
    {
        public int Id { get; set; }
        public string InterlocutorName { get; set; } = null!;
        public string AdvertisementTitle { get; set; } = null!;
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
