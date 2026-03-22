using FunApi.Interfaces;
using FunDto.Models.Contracts.Auth;
using FunDto.Models.Internal.Auth;
using FunTest.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunTest.BlackBox
{
    public class AuthControllerBlackBoxTests
    {
        [Fact]
        public async Task Register_ReturnsOk_WhenUserCreated()
        {
            var authService = new Mock<IAuthService>();
            authService
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterInternalDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResultInternalDto.Success(new AuthUserInternalDto
                {
                    Id = 1,
                    Email = "user@university.ru",
                    FullName = "Test User",
                    PhoneNumber = "+79991234567",
                    Role = "user"
                }));

            var controller = AuthControllerTestFactory.Create(authService);

            var result = await controller.Register(new RegisterRequestDto
            {
                Email = "user@university.ru",
                Password = "secret123",
                ConfirmPassword = "secret123",
                FullName = "Test User",
                Phone = "+79991234567",
                University = "Test University",
                Faculty = "Test Faculty"
            }, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponseDto>(okResult.Value);

            Assert.True(response.IsSuccess);
            Assert.NotNull(response.User);
            Assert.Equal("user@university.ru", response.User!.Email);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsNoContent()
        {
            var authService = new Mock<IAuthService>();
            authService
                .Setup(x => x.RequestPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var controller = AuthControllerTestFactory.Create(authService);

            var result = await controller.ForgotPassword(new ForgotPasswordRequestDto
            {
                Email = "user@university.ru"
            }, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
