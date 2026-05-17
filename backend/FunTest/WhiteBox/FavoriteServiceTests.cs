using FunApi.Services;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace FunTest.WhiteBox
{
    public class FavoriteServiceTests
    {
        [Fact]
        public async Task AddAsync_AddsFavoriteForApprovedAdvertisementOnlyOnce()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var service = new FavoriteService(context);

            await service.AddAsync(user.Id, advertisement.Id);
            await service.AddAsync(user.Id, advertisement.Id);

            Assert.Single(await context.Favorites.ToListAsync());
            Assert.True(await service.ExistsAsync(user.Id, advertisement.Id));
        }

        [Fact]
        public async Task AddAsync_Throws_WhenAdvertisementIsArchived()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, isArchived: true);
            var service = new FavoriteService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddAsync(user.Id, advertisement.Id));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyVisibleFavoritesNewestFirst()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var oldVisible = await TestData.AddAdvertisementAsync(context, seller, title: "Old", createdAt: DateTime.UtcNow.AddDays(-2));
            var newVisible = await TestData.AddAdvertisementAsync(context, seller, title: "New", createdAt: DateTime.UtcNow);
            var hidden = await TestData.AddAdvertisementAsync(context, seller, title: "Hidden", statusName: "pending");
            await TestData.AddFavoriteAsync(context, user, oldVisible);
            await TestData.AddFavoriteAsync(context, user, newVisible);
            await TestData.AddFavoriteAsync(context, user, hidden);
            var service = new FavoriteService(context);

            var result = await service.GetAllAsync(user.Id);

            Assert.Equal(["New", "Old"], result.Select(x => x.Title).ToArray());
            Assert.All(result, x => Assert.True(x.IsFavorite));
        }

        [Fact]
        public async Task RemoveAsync_RemovesFavorite_WhenItExists()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            await TestData.AddFavoriteAsync(context, user, advertisement);
            var service = new FavoriteService(context);

            await service.RemoveAsync(user.Id, advertisement.Id);

            Assert.False(await service.ExistsAsync(user.Id, advertisement.Id));
        }
    }
}
