using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Services;
using FunDto.Models.Contracts.Admin;
using FunTest.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FunTest.WhiteBox
{
    public class AdminServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_AppliesSearchAndBlockedFilters()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            await TestData.AddUserAsync(context, "active@test.edu", "Active User");
            await TestData.AddUserAsync(context, "blocked@test.edu", "Blocked User", isBlocked: true);
            var service = CreateService(context);

            var result = await service.GetUsersAsync(admin.Id, new UserAdminFilterDto
            {
                Search = "blocked",
                IsBlocked = true
            });

            var item = Assert.Single(result);
            Assert.Equal("Blocked User", item.FullName);
            Assert.True(item.IsBlocked);
        }

        [Fact]
        public async Task BlockUserAsync_BlocksRegularUser()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            var user = await TestData.AddUserAsync(context, "user@test.edu", "User");
            var service = CreateService(context);

            await service.BlockUserAsync(admin.Id, user.Id);

            Assert.True(await context.Users.Where(x => x.Id == user.Id).Select(x => x.IsBlocked).SingleAsync());
        }

        [Fact]
        public async Task BlockUserAsync_RejectsSelfBlock()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            var service = CreateService(context);

            await Assert.ThrowsAsync<DomainValidationException>(() => service.BlockUserAsync(admin.Id, admin.Id));
        }

        [Fact]
        public async Task BlockUserAsync_RejectsBlockingAnotherAdmin()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            var otherAdmin = await TestData.AddUserAsync(context, "other-admin@test.edu", "Other Admin", "admin");
            var service = CreateService(context);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.BlockUserAsync(admin.Id, otherAdmin.Id));
        }

        [Fact]
        public async Task GetStatsAsync_ReturnsSystemCounters()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            var buyer = await TestData.AddUserAsync(context, "buyer@test.edu", "Buyer", isBlocked: true);
            var seller = await TestData.AddUserAsync(context, "seller@test.edu", "Seller");
            var approved = await TestData.AddAdvertisementAsync(context, seller, statusName: "approved");
            await TestData.AddAdvertisementAsync(context, seller, statusName: "pending");
            await TestData.AddOrderAsync(context, buyer, seller, approved, "completed");
            var service = CreateService(context);

            var stats = await service.GetStatsAsync(admin.Id);

            Assert.Equal(3, stats.TotalUsers);
            Assert.Equal(1, stats.BlockedUsers);
            Assert.Equal(1, stats.ActiveAdvertisements);
            Assert.Equal(1, stats.CompletedOrders);
        }

        [Fact]
        public async Task ExportUsersCsvAsync_IncludesHeaderAndUsers()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin", "admin");
            await TestData.AddUserAsync(context, "user@test.edu", "User");
            var service = CreateService(context);

            var csv = await service.ExportUsersCsvAsync(admin.Id);

            Assert.Contains("Id,FullName,Email,Role,CreatedAt,IsBlocked", csv);
            Assert.Contains("\"user@test.edu\"", csv);
        }

        private static AdminService CreateService(FunApi.Models.FunDBcontext context)
        {
            var accessControl = new Mock<IAccessControlService>();
            accessControl
                .Setup(x => x.EnsureAnyRoleAsync(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            return new AdminService(context, accessControl.Object);
        }
    }
}
