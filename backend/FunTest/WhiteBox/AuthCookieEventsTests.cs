using FunApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FunTest.WhiteBox
{
    public class AuthCookieEventsTests
    {
        [Fact]
        public async Task RedirectToLogin_ReturnsUnauthorizedInsteadOfRedirect()
        {
            var events = new AuthCookieEvents(null!);
            var context = CreateRedirectContext("/login");

            await events.RedirectToLogin(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task RedirectToAccessDenied_ReturnsForbiddenInsteadOfRedirect()
        {
            var events = new AuthCookieEvents(null!);
            var context = CreateRedirectContext("/access-denied");

            await events.RedirectToAccessDenied(context);

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        private static RedirectContext<CookieAuthenticationOptions> CreateRedirectContext(string redirectUri)
        {
            var httpContext = new DefaultHttpContext();
            var scheme = new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme,
                typeof(CookieAuthenticationHandler));

            return new RedirectContext<CookieAuthenticationOptions>(
                httpContext,
                scheme,
                new CookieAuthenticationOptions(),
                new AuthenticationProperties(),
                redirectUri);
        }
    }
}
