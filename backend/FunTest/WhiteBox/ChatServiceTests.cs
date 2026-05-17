using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Services;
using FunDto.Models.Contracts.Chats;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class ChatServiceTests
    {
        [Fact]
        public async Task GetOrCreateAsync_CreatesBuyerChatForApprovedAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var service = CreateService(context);

            var result = await service.GetOrCreateAsync(buyer.Id, advertisement.Id);

            Assert.Equal(advertisement.Id, result.AdvertisementId);
            Assert.Equal(seller.Id, result.InterlocutorId);
            Assert.Single(await context.Chats.ToListAsync());
        }

        [Fact]
        public async Task GetOrCreateAsync_RejectsBuyerChatForPendingAdvertisement()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "pending");
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetOrCreateAsync(buyer.Id, advertisement.Id));
        }

        [Fact]
        public async Task GetOrCreateAsync_AllowsSellerChatOnlyForExistingBuyerOrder()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            await TestData.AddOrderAsync(context, buyer, seller, advertisement);
            var service = CreateService(context);

            var result = await service.GetOrCreateAsync(seller.Id, advertisement.Id, buyer.Id);

            Assert.Equal(buyer.Id, result.InterlocutorId);
        }

        [Fact]
        public async Task GetOrCreateAsync_RejectsSellerChatWithoutOrder()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var service = CreateService(context);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetOrCreateAsync(seller.Id, advertisement.Id, buyer.Id));
        }

        [Fact]
        public async Task SendMessageAsync_TrimsMessageAndNotifiesRecipient()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var chat = await TestData.AddChatAsync(context, advertisement, buyer, seller);
            var notifications = new Mock<INotificationService>();
            var service = CreateService(context, notifications);

            var result = await service.SendMessageAsync(buyer.Id, new SendMessageDto
            {
                ChatId = chat.Id,
                Content = "  Hello there  "
            });

            Assert.Equal("Hello there", result.Content);
            Assert.False(result.IsRead);
            notifications.Verify(x => x.CreateAsync(seller.Id, "new_message", "New message", "Hello there"), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_MarksOnlyMessagesFromOtherParticipant()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var chat = await TestData.AddChatAsync(context, advertisement, buyer, seller);
            var incoming = await TestData.AddMessageAsync(context, chat, seller, "Incoming", false);
            var own = await TestData.AddMessageAsync(context, chat, buyer, "Own", false);
            var service = CreateService(context);

            await service.MarkAsReadAsync(buyer.Id, chat.Id);

            Assert.True(await context.Messages.Where(x => x.Id == incoming.Id).Select(x => x.IsRead).SingleAsync());
            Assert.False(await context.Messages.Where(x => x.Id == own.Id).Select(x => x.IsRead).SingleAsync());
        }

        private static ChatService CreateService(
            FunApi.Models.FunDBcontext context,
            Mock<INotificationService>? notifications = null)
        {
            notifications ??= new Mock<INotificationService>();
            return new ChatService(context, notifications.Object);
        }
    }
}
