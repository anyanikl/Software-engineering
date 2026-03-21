namespace FunDto.Models.Contracts.Chats
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
