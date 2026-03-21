using FunApi.Models.Users;

namespace FunApi.Models.Chats
{
    public class Message
    {
        public int Id { get; set; }

        public int ChatId { get; set; }
        public int SenderId { get; set; }

        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public Chat Chat { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
