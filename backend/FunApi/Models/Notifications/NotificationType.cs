namespace FunApi.Models.Notifications
{
    public class NotificationType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
