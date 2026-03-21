using FunDto.Models.Contracts.Notifications;

namespace FunApi.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetMyNotificationsAsync(int userId);
        Task MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task CreateAsync(int userId, string type, string title, string content);
    }
}
