using FunApi.Models.Users;

namespace FunApi.Models.Notifications
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int NotificationTypeId { get; set; }

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public NotificationType NotificationType { get; set; } = null!;
    }
}
