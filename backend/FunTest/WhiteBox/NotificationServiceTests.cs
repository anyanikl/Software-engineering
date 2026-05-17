using FunApi.Models.Notifications;
using FunApi.Services;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace FunTest.WhiteBox
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task CreateAsync_CreatesNotificationTypeAndUnreadNotification()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Regular User");
            var service = new NotificationService(context);

            await service.CreateAsync(user.Id, "new_order", "New order", "Order body");

            var notification = await context.Notifications.Include(x => x.NotificationType).SingleAsync();
            Assert.Equal(user.Id, notification.UserId);
            Assert.Equal("new_order", notification.NotificationType.Name);
            Assert.False(notification.IsRead);
            Assert.Equal(1, await service.GetUnreadCountAsync(user.Id));
        }

        [Fact]
        public async Task MarkAsReadAsync_MarksOnlyRequestedUsersNotification()
        {
            await using var context = TestDbContextFactory.Create();
            var owner = await TestData.AddUserAsync(context, "owner@test.edu", "Owner");
            var other = await TestData.AddUserAsync(context, "other@test.edu", "Other");
            var type = await TestData.AddNotificationTypeAsync(context, "system");
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, NotificationTypeId = type.Id, Title = "One", Content = "Body", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { UserId = other.Id, NotificationTypeId = type.Id, Title = "Two", Content = "Body", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            var ownerNotificationId = await context.Notifications.Where(x => x.UserId == owner.Id).Select(x => x.Id).SingleAsync();
            var service = new NotificationService(context);

            await service.MarkAsReadAsync(owner.Id, ownerNotificationId);

            Assert.Equal(0, await service.GetUnreadCountAsync(owner.Id));
            Assert.Equal(1, await service.GetUnreadCountAsync(other.Id));
        }

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllUnreadNotificationsForUser()
        {
            await using var context = TestDbContextFactory.Create();
            var owner = await TestData.AddUserAsync(context, "owner@test.edu", "Owner");
            var type = await TestData.AddNotificationTypeAsync(context, "system");
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, NotificationTypeId = type.Id, Title = "One", Content = "Body", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { UserId = owner.Id, NotificationTypeId = type.Id, Title = "Two", Content = "Body", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            var service = new NotificationService(context);

            await service.MarkAllAsReadAsync(owner.Id);

            Assert.Equal(0, await service.GetUnreadCountAsync(owner.Id));
        }

        [Fact]
        public async Task GetMyNotificationsAsync_ReturnsNewestFirstWithType()
        {
            await using var context = TestDbContextFactory.Create();
            var owner = await TestData.AddUserAsync(context, "owner@test.edu", "Owner");
            var type = await TestData.AddNotificationTypeAsync(context, "new_message");
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, NotificationTypeId = type.Id, Title = "Old", Content = "First", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new Notification { UserId = owner.Id, NotificationTypeId = type.Id, Title = "New", Content = "Second", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            var service = new NotificationService(context);

            var result = await service.GetMyNotificationsAsync(owner.Id);

            Assert.Equal(["New", "Old"], result.Select(x => x.Title).ToArray());
            Assert.All(result, x => Assert.Equal("new_message", x.Type));
        }
    }
}
