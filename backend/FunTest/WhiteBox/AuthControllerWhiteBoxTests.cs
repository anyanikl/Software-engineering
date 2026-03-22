using FunApi.Interfaces;
using FunDto.Models.Contracts.Auth;
using FunDto.Models.Internal.Auth;
using FunTest.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunTest.WhiteBox
{
    public class AuthControllerWhiteBoxTests
    {
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenServiceReturnsFailure()
        {
            var authService = new Mock<IAuthService>();
            authService
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterInternalDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResultInternalDto.Failure("User with this email already exists"));

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

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponseDto>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Contains("User with this email already exists", response.Errors);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenServiceThrowsArgumentException()
        {
            var authService = new Mock<IAuthService>();
            authService
                .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Passwords do not match"));

            var controller = AuthControllerTestFactory.Create(authService);

            var result = await controller.ResetPassword(new ResetPasswordRequestDto
            {
                Token = "token",
                NewPassword = "secret123",
                ConfirmPassword = "different123"
            }, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }
    }
}
