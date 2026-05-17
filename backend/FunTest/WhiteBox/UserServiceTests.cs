using FunApi.Exceptions;
using FunApi.Services;
using FunDto.Models.Contracts.Users;
using FunTest.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class UserServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_ReturnsProfileCounters()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var advertisement = await TestData.AddAdvertisementAsync(context, seller, statusName: "approved");
            await TestData.AddOrderAsync(context, buyer, seller, advertisement, "completed");
            var service = CreateService(context);

            var result = await service.GetByIdAsync(seller.Id);

            Assert.Equal("Seller", result.FullName);
            Assert.Equal(1, result.SalesCount);
            Assert.Equal(1, result.ActiveAdvertisementsCount);
        }

        [Fact]
        public async Task UpdateProfileAsync_UpdatesNamePhoneAndFaculty()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var university = await context.Universities.SingleAsync();
            await TestData.AddFacultyAsync(context, university, "Math");
            var service = CreateService(context);

            var result = await service.UpdateProfileAsync(user.Id, new UpdateUserProfileDto
            {
                FullName = "  Updated User  ",
                Phone = "  +79991112233  ",
                Faculty = "Math"
            });

            Assert.Equal("Updated User", result.FullName);
            Assert.Equal("+79991112233", result.Phone);
            Assert.Equal("Math", result.Faculty);
        }

        [Fact]
        public async Task UpdateProfileAsync_Throws_WhenFacultyDoesNotBelongToUserUniversity()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var otherUniversity = await TestData.AddUniversityAsync(context, "Other University", "other.edu");
            await TestData.AddFacultyAsync(context, otherUniversity, "Other Faculty");
            var service = CreateService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.UpdateProfileAsync(user.Id, new UpdateUserProfileDto
            {
                FullName = "User",
                Phone = "+79991112233",
                Faculty = "Other Faculty"
            }));
        }

        [Fact]
        public async Task GetPublicProfileAsync_ReturnsReviewsAndOnlyActiveAdvertisements()
        {
            await using var context = TestDbContextFactory.Create();
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer");
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var active = await TestData.AddAdvertisementAsync(context, seller, statusName: "approved", title: "Active");
            await TestData.AddAdvertisementAsync(context, seller, statusName: "pending", title: "Hidden");
            var order = await TestData.AddOrderAsync(context, buyer, seller, active, "completed");
            await TestData.AddReviewAsync(context, order, buyer, seller, 5, "Excellent");
            seller.Rating = 5;
            seller.ReviewsCount = 1;
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetPublicProfileAsync(seller.Id);

            Assert.Equal("Seller", result.FullName);
            Assert.Equal("Excellent", result.Reviews.Single().Comment);
            Assert.Equal("Active", result.ActiveAdvertisements.Single().Title);
        }

        [Fact]
        public async Task DeleteAvatarAsync_RemovesAvatarUrl()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            user.AvatarUrl = "/uploads/avatars/1/missing.jpg";
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteAvatarAsync(user.Id);

            Assert.Null(await context.Users.Where(x => x.Id == user.Id).Select(x => x.AvatarUrl).SingleAsync());
        }

        private static UserService CreateService(FunApi.Models.FunDBcontext context)
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());
            environment.SetupGet(x => x.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "fun-user-tests"));

            return new UserService(context, environment.Object);
        }
    }
}
