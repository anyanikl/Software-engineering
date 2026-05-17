using FunApi;
using FunApi.Exceptions;
using FunApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace FunTest.WhiteBox
{
    public class ProgramConfigurationTests
    {
        [Fact]
        public void ResolveAllowedOrigins_ReturnsDefaults_WhenConfigMissing()
        {
            var configuration = new ConfigurationBuilder().Build();

            var origins = Program.ResolveAllowedOrigins(configuration);

            Assert.Equal(new[]
            {
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "https://localhost",
                "https://127.0.0.1"
            }, origins);
        }

        [Fact]
        public void ResolveAllowedOrigins_TrimsAndDeduplicatesConfiguredOrigins()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CORS_ALLOWED_ORIGINS"] = " https://site.test,https://site.test, HTTPS://SITE.TEST , http://localhost:3000 "
                })
                .Build();

            var origins = Program.ResolveAllowedOrigins(configuration);

            Assert.Equal(new[]
            {
                "https://site.test",
                "http://localhost:3000"
            }, origins);
        }

        [Fact]
        public void ConfigureForwardedHeaders_EnablesProxyHeadersAndClearsTrustedDefaults()
        {
            var options = new ForwardedHeadersOptions();

            Program.ConfigureForwardedHeaders(options);

            var expectedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            Assert.Equal(expectedHeaders, options.ForwardedHeaders);
            Assert.Empty(options.KnownNetworks);
            Assert.Empty(options.KnownProxies);
        }

        [Fact]
        public void ConfigureApplicationCookie_UsesHardenedSessionDefaults()
        {
            var options = new CookieAuthenticationOptions();

            Program.ConfigureApplicationCookie(options);

            Assert.Equal(".AspNetCore.Cookies", options.Cookie.Name);
            Assert.True(options.Cookie.HttpOnly);
            Assert.Equal("/", options.Cookie.Path);
            Assert.Equal(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);
            Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
            Assert.Equal(TimeSpan.FromDays(30), options.ExpireTimeSpan);
            Assert.True(options.SlidingExpiration);
            Assert.Equal(typeof(AuthCookieEvents), options.EventsType);
        }

        [Fact]
        public void ConfigureRateLimiter_AllowsOnlyOneHundredRequestsPerWindow()
        {
            var options = new RateLimiterOptions();

            Program.ConfigureRateLimiter(options);

            Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
            Assert.NotNull(options.GlobalLimiter);

            using var acceptedLease = options.GlobalLimiter!.AttemptAcquire(new DefaultHttpContext(), 100);
            using var rejectedLease = options.GlobalLimiter.AttemptAcquire(new DefaultHttpContext());

            Assert.True(acceptedLease.IsAcquired);
            Assert.False(rejectedLease.IsAcquired);
        }

        [Theory]
        [MemberData(nameof(ExceptionStatusCodeCases))]
        public void ResolveExceptionStatusCode_MapsKnownExceptions(Exception exception, int expectedStatusCode)
        {
            var statusCode = Program.ResolveExceptionStatusCode(exception);

            Assert.Equal(expectedStatusCode, statusCode);
        }

        public static IEnumerable<object[]> ExceptionStatusCodeCases()
        {
            yield return new object[] { new ForbiddenException("Access denied"), StatusCodes.Status403Forbidden };
            yield return new object[] { new KeyNotFoundException("Missing"), StatusCodes.Status404NotFound };
            yield return new object[] { new DomainValidationException("Invalid domain state"), StatusCodes.Status400BadRequest };
            yield return new object[] { new ArgumentException("Invalid argument"), StatusCodes.Status400BadRequest };
            yield return new object[] { new InvalidOperationException("Unexpected"), StatusCodes.Status500InternalServerError };
        }
    }
}
