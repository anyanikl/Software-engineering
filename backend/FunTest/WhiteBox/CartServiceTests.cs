using FunApi.Services;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace FunTest.WhiteBox
{
    public class CartServiceTests
    {
        [Fact]
        public async Task AddItemAsync_AddsApprovedAdvertisementOnlyOnce()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, price: 250);
            var service = new CartService(context);

            await service.AddItemAsync(buyer.Id, advertisement.Id);
            await service.AddItemAsync(buyer.Id, advertisement.Id);

            var cart = await context.Carts.Include(x => x.Items).SingleAsync(x => x.UserId == buyer.Id);
            Assert.Single(cart.Items);
        }

        [Fact]
        public async Task AddItemAsync_Throws_WhenAdvertisementIsNotApproved()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending");
            var service = new CartService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddItemAsync(buyer.Id, advertisement.Id));
        }

        [Fact]
        public async Task GetCartAsync_ReturnsOnlyVisibleItemsAndTotals()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var approved = await TestData.AddAdvertisementAsync(context, seller, title: "Approved", price: 150);
            var archived = await TestData.AddAdvertisementAsync(context, seller, title: "Archived", price: 900, isArchived: true);
            var cart = await TestData.AddCartAsync(context, buyer);
            await TestData.AddCartItemAsync(context, cart, approved);
            await TestData.AddCartItemAsync(context, cart, archived);
            var service = new CartService(context);

            var result = await service.GetCartAsync(buyer.Id);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(150, result.TotalPrice);
            Assert.Equal("Approved", result.Items.Single().Title);
            Assert.True(result.Items.Single().IsInCart);
        }

        [Fact]
        public async Task RemoveItemAsync_RemovesOnlyRequestedItem()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var first = await TestData.AddAdvertisementAsync(context, seller, title: "First");
            var second = await TestData.AddAdvertisementAsync(context, seller, title: "Second");
            var cart = await TestData.AddCartAsync(context, buyer);
            await TestData.AddCartItemAsync(context, cart, first);
            await TestData.AddCartItemAsync(context, cart, second);
            var service = new CartService(context);

            await service.RemoveItemAsync(buyer.Id, first.Id);

            var remainingIds = await context.CartItems.Select(x => x.AdvertisementId).ToListAsync();
            Assert.Equal([second.Id], remainingIds);
        }

        [Fact]
        public async Task ClearAsync_RemovesAllItemsForUsersCart()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var cart = await TestData.AddCartAsync(context, buyer);
            await TestData.AddCartItemAsync(context, cart, advertisement);
            var service = new CartService(context);

            await service.ClearAsync(buyer.Id);

            Assert.Empty(await context.CartItems.ToListAsync());
        }
    }
}
