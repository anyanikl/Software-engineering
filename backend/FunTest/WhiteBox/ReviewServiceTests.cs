using FunApi.Exceptions;
using FunApi.Services;
using FunDto.Models.Contracts.Reviews;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace FunTest.WhiteBox
{
    public class ReviewServiceTests
    {
        [Fact]
        public async Task CanLeaveReviewAsync_ReturnsTrue_ForCompletedOrderParticipantWithoutReview()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            var service = new ReviewService(context);

            var result = await service.CanLeaveReviewAsync(buyer.Id, order.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task CanLeaveReviewAsync_ReturnsFalse_WhenOrderIsNotCompleted()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "pending");
            var service = new ReviewService(context);

            var result = await service.CanLeaveReviewAsync(buyer.Id, order.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_CreatesReviewForCounterpartyAndUpdatesRating()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, title: "Desk");
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            var service = new ReviewService(context);

            var result = await service.CreateAsync(buyer.Id, new CreateReviewDto
            {
                OrderId = order.Id,
                Rating = 4,
                Comment = "  Good seller  "
            });

            Assert.Equal("Buyer", result.AuthorName);
            Assert.Equal("Desk", result.ProductName);
            Assert.Equal("Good seller", result.Comment);

            var updatedSeller = await context.Users.SingleAsync(x => x.Id == seller.Id);
            Assert.Equal(1, updatedSeller.ReviewsCount);
            Assert.Equal(4, updatedSeller.Rating);
        }

        [Fact]
        public async Task CreateAsync_RejectsDuplicateReviewBySameAuthor()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            await TestData.AddReviewAsync(context, order, buyer, seller);
            var service = new ReviewService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(buyer.Id, new CreateReviewDto
            {
                OrderId = order.Id,
                Rating = 5
            }));
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsReviewsForTargetUserNewestFirst()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller);
            var order = await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            var secondAdvertisement = await TestData.AddAdvertisementAsync(context, seller, title: "Second item");
            var secondOrder = await TestData.AddOrderAsync(context, buyer, seller, secondAdvertisement, "completed");
            var older = await TestData.AddReviewAsync(context, order, buyer, seller, 3, "Older");
            older.CreatedAt = DateTime.UtcNow.AddDays(-1);
            var newer = await TestData.AddReviewAsync(context, secondOrder, buyer, seller, 5, "Newer");
            newer.CreatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            var service = new ReviewService(context);

            var result = await service.GetByUserIdAsync(seller.Id);

            Assert.Equal(["Newer", "Older"], result.Select(x => x.Comment!).ToArray());
        }
    }
}
