using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Notifications;
using FunDto.Models.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FunDBcontext _context;

        public NotificationService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Include(x => x.NotificationType)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new NotificationDto
                {
                    Id = x.Id,
                    Type = x.NotificationType.Name,
                    Title = x.Title,
                    Content = x.Content,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == notificationId);
            if (notification is null) return;
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public Task<int> GetUnreadCountAsync(int userId)
        {
            return _context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead);
        }

        public async Task CreateAsync(int userId, string type, string title, string content)
        {
            var typeId = await EnsureNotificationTypeAsync(type);
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                NotificationTypeId = typeId,
                Title = title,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private async Task<int> EnsureNotificationTypeAsync(string name)
        {
            var type = await _context.NotificationTypes.FirstOrDefaultAsync(x => x.Name == name);
            if (type is not null) return type.Id;

            type = new NotificationType { Name = name };
            _context.NotificationTypes.Add(type);
            await _context.SaveChangesAsync();
            return type.Id;
        }
    }
}
