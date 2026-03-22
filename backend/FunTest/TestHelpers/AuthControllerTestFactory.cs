using FunApi.Controllers;
using FunApi.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunTest.TestHelpers
{
    internal static class AuthControllerTestFactory
    {
        public static AuthController Create(Mock<IAuthService> authService)
        {
            var antiforgery = new Mock<IAntiforgery>();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns("Development");

            return new AuthController(
                authService.Object,
                antiforgery.Object,
                environment.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }
    }
}
