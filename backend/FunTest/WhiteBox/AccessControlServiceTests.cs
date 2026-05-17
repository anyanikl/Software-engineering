using FunApi.Exceptions;
using FunApi.Services;
using FunTest.TestHelpers;

namespace FunTest.WhiteBox
{
    public class AccessControlServiceTests
    {
        [Fact]
        public async Task HasAnyRoleAsync_ReturnsTrue_WhenUserRoleMatchesIgnoringCase()
        {
            await using var context = TestDbContextFactory.Create();
            var admin = await TestData.AddUserAsync(context, "admin@test.edu", "Admin User", "Admin");
            var service = new AccessControlService(context);

            var result = await service.HasAnyRoleAsync(admin.Id, " moderator ", "ADMIN");

            Assert.True(result);
        }

        [Fact]
        public async Task HasAnyRoleAsync_ReturnsFalse_WhenNoRolesProvided()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Regular User");
            var service = new AccessControlService(context);

            var result = await service.HasAnyRoleAsync(user.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task EnsureAnyRoleAsync_ThrowsForbidden_WhenRoleDoesNotMatch()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Regular User");
            var service = new AccessControlService(context);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureAnyRoleAsync(user.Id, "admin"));
        }
    }
}
