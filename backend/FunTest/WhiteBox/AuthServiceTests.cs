using System.Text.RegularExpressions;
using FunApi.Interfaces;
using FunApi.Models.Users;
using FunApi.Services;
using FunDto.Models.Internal.Auth;
using FunTest.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunTest.WhiteBox
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task RegisterAsync_CreatesUserCartAndSendsConfirmationEmail()
        {
            await using var context = TestDbContextFactory.Create();
            var emailSender = new Mock<IEmailSender>();
            var service = CreateService(context, emailSender);

            var result = await service.RegisterAsync(new RegisterInternalDto
            {
                Email = "  NewUser@Test.EDU  ",
                Password = "password1",
                ConfirmPassword = "password1",
                FullName = "  New User  ",
                PhoneNumber = "  +79991112233  ",
                University = "Test University",
                Faculty = "Engineering"
            });

            Assert.True(result.IsSuccess);
            Assert.Equal("newuser@test.edu", result.User!.Email);
            Assert.True(await context.Users.AnyAsync(x => x.Email == "newuser@test.edu" && !x.IsEmailConfirmed));
            Assert.True(await context.Carts.AnyAsync());
            emailSender.Verify(x => x.SendAsync("newuser@test.edu", "Confirm your FinPay email", It.Is<string>(body => body.Contains("confirm-email.html")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsFailure_WhenEmailAlreadyExists()
        {
            await using var context = TestDbContextFactory.Create();
            await TestData.AddUserAsync(context, "user@test.edu", "Existing User");
            var service = CreateService(context);

            var result = await service.RegisterAsync(new RegisterInternalDto
            {
                Email = "USER@test.edu",
                Password = "password1",
                ConfirmPassword = "password1",
                FullName = "New User",
                PhoneNumber = "+79991112233",
                University = "Test University",
                Faculty = "Engineering"
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("User with this email already exists", result.Errors);
        }

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenPasswordMatchesConfirmedUser()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Existing User");
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "password1");
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.LoginAsync(new LoginInternalDto
            {
                Email = " USER@test.edu ",
                Password = "password1"
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(user.Id, result.User!.Id);
            Assert.Equal("user", result.User.Role);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenEmailIsNotConfirmed()
        {
            await using var context = TestDbContextFactory.Create();
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Existing User", isEmailConfirmed: false);
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "password1");
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.LoginAsync(new LoginInternalDto
            {
                Email = "user@test.edu",
                Password = "password1"
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("Email is not confirmed", result.Errors);
        }

        [Fact]
        public async Task RequestPasswordResetAndResetPasswordAsync_UpdatePasswordAndClearToken()
        {
            await using var context = TestDbContextFactory.Create();
            var emailBody = "";
            var emailSender = new Mock<IEmailSender>();
            emailSender
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, body, _) => emailBody = body)
                .Returns(Task.CompletedTask);
            var user = await TestData.AddUserAsync(context, "user@test.edu", "Existing User");
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "password1");
            await context.SaveChangesAsync();
            var service = CreateService(context, emailSender);

            await service.RequestPasswordResetAsync(" USER@test.edu ");
            await service.ResetPasswordAsync(ExtractToken(emailBody), "newpass1", "newpass1");

            var updatedUser = await context.Users.SingleAsync(x => x.Id == user.Id);
            Assert.Null(updatedUser.PasswordResetTokenHash);
            Assert.Null(updatedUser.PasswordResetTokenExpiresAt);

            var login = await service.LoginAsync(new LoginInternalDto
            {
                Email = "user@test.edu",
                Password = "newpass1"
            });
            Assert.True(login.IsSuccess);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ConfirmsUserFromRegistrationToken()
        {
            await using var context = TestDbContextFactory.Create();
            var emailBody = "";
            var emailSender = new Mock<IEmailSender>();
            emailSender
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, body, _) => emailBody = body)
                .Returns(Task.CompletedTask);
            var service = CreateService(context, emailSender);

            await service.RegisterAsync(new RegisterInternalDto
            {
                Email = "user@test.edu",
                Password = "password1",
                ConfirmPassword = "password1",
                FullName = "User",
                PhoneNumber = "+79991112233",
                University = "Test University",
                Faculty = "Engineering"
            });

            context.ChangeTracker.Clear();
            await service.ConfirmEmailAsync(ExtractToken(emailBody));

            var user = await context.Users.SingleAsync();
            Assert.True(user.IsEmailConfirmed);
            Assert.Null(user.EmailConfirmationTokenHash);
        }

        private static AuthService CreateService(FunApi.Models.FunDBcontext context, Mock<IEmailSender>? emailSender = null)
        {
            if (emailSender is null)
            {
                emailSender = new Mock<IEmailSender>();
                emailSender
                    .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://frontend.test"
                })
                .Build();

            return new AuthService(
                context,
                Mock.Of<ILogger<AuthService>>(),
                emailSender.Object,
                configuration);
        }

        private static string ExtractToken(string htmlBody)
        {
            var match = Regex.Match(htmlBody, @"token=([^""<]+)");
            Assert.True(match.Success, "Expected email body to contain a token query parameter.");
            return Uri.UnescapeDataString(match.Groups[1].Value);
        }
    }
}
