using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Services;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class OrderServiceTests
    {
        [Fact]
        public async Task CreateSingleAsync_CreatesPendingOrderAndNotifiesSeller()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, price: 700);
            var notifications = new Mock<INotificationService>();
            var service = CreateService(context, notifications: notifications);

            var result = await service.CreateSingleAsync(buyer.Id, advertisement.Id);

            Assert.Equal("pending", result.Status);
            Assert.Equal(700, result.Price);
            notifications.Verify(x => x.CreateAsync(seller.Id, "new_order", "New order", It.Is<string>(body => body.Contains(advertisement.Title))), Times.Once);
        }

        [Fact]
        public async Task CreateSingleAsync_RejectsSellerBuyingOwnAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var service = CreateService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateSingleAsync(seller.Id, advertisement.Id));
        }

        [Fact]
        public async Task CreateFromCartAsync_CreatesOrdersForEveryCartItemAndClearsCart()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var first = await TestData.AddAdvertisementAsync(context, seller, title: "First", price: 100);
            var second = await TestData.AddAdvertisementAsync(context, seller, title: "Second", price: 200);
            var cart = await TestData.AddCartAsync(context, buyer);
            await TestData.AddCartItemAsync(context, cart, first);
            await TestData.AddCartItemAsync(context, cart, second);
            var cartService = new Mock<ICartService>();
            var notifications = new Mock<INotificationService>();
            var service = CreateService(context, cartService, notifications);

            var result = await service.CreateFromCartAsync(buyer.Id);

            Assert.Equal(second.Id, result.AdvertisementId);
            Assert.Equal(2, await context.Orders.CountAsync());
            cartService.Verify(x => x.ClearAsync(buyer.Id), Times.Once);
            notifications.Verify(x => x.CreateAsync(seller.Id, "new_order", "New order", It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateFromCartAsync_Throws_WhenCartIsEmpty()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            await TestData.AddCartAsync(context, buyer);
            var service = CreateService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateFromCartAsync(buyer.Id));
        }

        [Fact]
        public async Task CompleteAsync_MarksOrderCompleted()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement);
            var service = CreateService(context);

            await service.CompleteAsync(buyer.Id, order.Id);

            var completedOrder = await context.Orders.Include(x => x.OrderStatus).SingleAsync(x => x.Id == order.Id);
            Assert.Equal("completed", completedOrder.OrderStatus.Name);
            Assert.NotNull(completedOrder.CompletedAt);
        }

        [Fact]
        public async Task CancelAsync_RejectsCompletedOrder()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            var service = CreateService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.CancelAsync(seller.Id, order.Id));
        }

        [Fact]
        public async Task GetByIdAsync_Throws_WhenUserIsNotParticipant()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var stranger = await TestData.AddUserAsync(context, "stranger@test.edu", "Stranger");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(stranger.Id, order.Id));
        }

        private static OrderService CreateService(
            FunApi.Models.FunDBcontext context,
            Mock<ICartService>? cartService = null,
            Mock<INotificationService>? notifications = null)
        {
            cartService ??= new Mock<ICartService>();
            notifications ??= new Mock<INotificationService>();
            return new OrderService(context, cartService.Object, notifications.Object);
        }
    }
}
