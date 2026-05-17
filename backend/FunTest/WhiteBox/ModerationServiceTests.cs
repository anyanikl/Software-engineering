using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Services;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class ModerationServiceTests
    {
        [Fact]
        public async Task GetPendingAsync_ReturnsOnlyPendingActiveAdvertisements()
        {
            await using var context = TestDbContextFactory.Create();
            var moderator = await TestData.AddUserAsync(context, "mod@test.edu", "Moderator", "moderator");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var pending = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending", title: "Pending");
            await TestData.AddAdvertisementAsync(context, seller, statusName: "approved", title: "Approved");
            await TestData.AddAdvertisementAsync(context, seller, statusName: "pending", title: "Archived", isArchived: true);
            await TestData.AddAdvertisementImageAsync(context, pending, "/uploads/pending.jpg");
            var service = CreateService(context);

            var result = await service.GetPendingAsync(moderator.Id);

            var item = Assert.Single(result);
            Assert.Equal("Pending", item.Title);
            Assert.Equal(["/uploads/pending.jpg"], item.ImageUrls);
        }

        [Fact]
        public async Task ApproveAsync_ChangesStatusAddsModerationRecordAndNotifiesSeller()
        {
            await using var context = TestDbContextFactory.Create();
            var moderator = await TestData.AddUserAsync(context, "mod@test.edu", "Moderator", "moderator");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending", title: "Laptop");
            var notifications = new Mock<INotificationService>();
            var service = CreateService(context, notifications: notifications);

            await service.ApproveAsync(moderator.Id, advertisement.Id, "Looks good");

            var updated = await context.Advertisements.Include(x => x.AdvertisementStatus).SingleAsync(x => x.Id == advertisement.Id);
            Assert.Equal("approved", updated.AdvertisementStatus.Name);
            Assert.Equal("Looks good", updated.ModeratorComment);
            Assert.Single(await context.AdvertisementModerations.ToListAsync());
            notifications.Verify(x => x.CreateAsync(seller.Id, "moderation", "Moderation update", "Laptop: approved"), Times.Once);
        }

        [Fact]
        public async Task RejectAsync_ThrowsForbidden_WhenModeratorOwnsAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var moderator = await TestData.AddUserAsync(context, "mod@test.edu", "Moderator", "moderator");
            var advertisement = await TestData.AddAdvertisementAsync(context, moderator, statusName: "pending");
            var service = CreateService(context);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.RejectAsync(moderator.Id, advertisement.Id, "No"));
        }

        [Fact]
        public async Task SendForRevisionAsync_CreatesRevisionStatusWhenMissing()
        {
            await using var context = TestDbContextFactory.Create();
            var moderator = await TestData.AddUserAsync(context, "mod@test.edu", "Moderator", "moderator");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending");
            var service = CreateService(context);

            await service.SendForRevisionAsync(moderator.Id, advertisement.Id, "Add photo");

            var updated = await context.Advertisements.Include(x => x.AdvertisementStatus).SingleAsync(x => x.Id == advertisement.Id);
            Assert.Equal("revision", updated.AdvertisementStatus.Name);
            Assert.Equal("Add photo", updated.ModeratorComment);
        }

        private static ModerationService CreateService(
            FunApi.Models.FunDBcontext context,
            Mock<IAccessControlService>? accessControl = null,
            Mock<INotificationService>? notifications = null)
        {
            accessControl ??= new Mock<IAccessControlService>();
            accessControl
                .Setup(x => x.EnsureAnyRoleAsync(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            notifications ??= new Mock<INotificationService>();
            return new ModerationService(context, notifications.Object, accessControl.Object);
        }
    }
}
