using FunApi.Interfaces;
using FunApi.Services;
using FunDto.Models.Contracts.Advertisements;
using FunTest.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class AdvertisementServiceTests
    {
        [Fact]
        public async Task SearchAsync_ReturnsOnlyApprovedVisibleAdvertisementsWithFilters()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            await TestData.AddAdvertisementAsync(context, seller, title: "Calculus Notes", price: 300);
            await TestData.AddAdvertisementAsync(context, seller, title: "Physics Book", price: 100);
            await TestData.AddAdvertisementAsync(context, seller, title: "Calculus Draft", statusName: "pending", price: 1);
            var service = CreateService(context);

            var result = await service.SearchAsync(new AdvertisementFilterDto
            {
                Search = "calculus",
                SortBy = "price_desc"
            });

            var item = Assert.Single(result);
            Assert.Equal("Calculus Notes", item.Title);
            Assert.Equal(300, item.Price);
        }

        [Fact]
        public async Task GetByIdAsync_HidesPendingAdvertisementFromRegularViewer()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var viewer = await TestData.AddUserAsync(context, "viewer@test.edu", "Viewer");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending");
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(advertisement.Id, viewer.Id));
        }

        [Fact]
        public async Task GetByIdAsync_AllowsOwnerToSeePendingAdvertisementAndModerationComment()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "rejected");
            advertisement.ModeratorComment = "Fix description";
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetByIdAsync(advertisement.Id, seller.Id);

            Assert.Equal("rejected", result.Status);
            Assert.Equal("Fix description", result.ModeratorComment);
            Assert.True(result.CanEdit);
        }

        [Fact]
        public async Task CreateAsync_NormalizesFieldsAndCreatesPendingAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var service = CreateService(context);

            var result = await service.CreateAsync(seller.Id, new CreateAdvertisementDto
            {
                Title = "  New Book  ",
                Description = "  Useful book  ",
                Course = 3,
                Type = "  Books  ",
                Price = 450,
                Location = "  Library  "
            });

            Assert.Equal("New Book", result.Title);
            Assert.Equal("pending", result.Status);
            Assert.True(result.CanEdit);
        }

        [Fact]
        public async Task UpdateAsync_ResetsModerationStateAndUnarchivesAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "rejected", isArchived: true);
            advertisement.ModeratorComment = "Bad image";
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateAsync(seller.Id, advertisement.Id, new UpdateAdvertisementDto
            {
                Title = "Updated",
                Description = "Updated description",
                Course = 1,
                Type = "Books",
                Price = 50,
                Location = "Room 1"
            });

            Assert.Equal("pending", result.Status);
            Assert.Null(result.ModeratorComment);
            Assert.False(await context.Advertisements.Where(x => x.Id == advertisement.Id).Select(x => x.IsArchived).SingleAsync());
        }

        [Fact]
        public async Task ArchiveRestoreAndDeleteAsync_UpdateAdvertisementLifecycle()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "approved");
            var service = CreateService(context);

            await service.ArchiveAsync(seller.Id, advertisement.Id);
            Assert.True(await context.Advertisements.Where(x => x.Id == advertisement.Id).Select(x => x.IsArchived).SingleAsync());

            await service.RestoreAsync(seller.Id, advertisement.Id);
            var restored = await context.Advertisements.Include(x => x.AdvertisementStatus).SingleAsync(x => x.Id == advertisement.Id);
            Assert.False(restored.IsArchived);
            Assert.Equal("pending", restored.AdvertisementStatus.Name);

            await service.DeleteAsync(seller.Id, advertisement.Id);
            Assert.True(await context.Advertisements.Where(x => x.Id == advertisement.Id).Select(x => x.IsDeleted).SingleAsync());
        }

        private static AdvertisementService CreateService(FunApi.Models.FunDBcontext context, bool hasElevatedAccess = false)
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());
            environment.SetupGet(x => x.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "fun-api-tests"));

            var accessControl = new Mock<IAccessControlService>();
            accessControl
                .Setup(x => x.HasAnyRoleAsync(It.IsAny<int>(), It.IsAny<string[]>()))
                .ReturnsAsync(hasElevatedAccess);

            return new AdvertisementService(context, environment.Object, accessControl.Object);
        }
    }
}
