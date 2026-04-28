using FunApi.Controllers;
using FunApi.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
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

            return new AuthController(
                authService.Object,
                antiforgery.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }
    }
}
