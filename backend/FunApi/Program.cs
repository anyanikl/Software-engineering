using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

namespace FunApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var isDevelopment = builder.Environment.IsDevelopment();
            var allowedOrigins = ResolveAllowedOrigins(builder.Configuration);

            builder.Services.Configure<ForwardedHeadersOptions>(ConfigureForwardedHeaders);

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<FunDBcontext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<AuthCookieEvents>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAppConfigService, AppConfigService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAdvertisementService, AdvertisementService>();
            builder.Services.AddScoped<IFavoriteService, FavoriteService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IModerationService, ModerationService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAccessControlService, AccessControlService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(ConfigureApplicationCookie);
            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "antiforgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.Path = "/";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            builder.Services.AddRateLimiter(ConfigureRateLimiter);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            if (string.IsNullOrWhiteSpace(builder.Environment.WebRootPath))
            {
                builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
            }
            Directory.CreateDirectory(Path.Combine(builder.Environment.WebRootPath, "uploads"));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FunDBcontext>();

                context.Database.Migrate();
            }

            if (isDevelopment)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseForwardedHeaders();
            app.UseRateLimiter();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    var statusCode = ResolveExceptionStatusCode(exception);

                    context.Response.StatusCode = statusCode;
                    if (statusCode != StatusCodes.Status500InternalServerError)
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = exception?.Message ?? "Request failed"
                        });

                        return;
                    }
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Server error",
                        message = "Произошла ошибка"
                    });
                });
            });

            app.UseRouting();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(app.Environment.WebRootPath),
                RequestPath = ""
            });
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapControllers();

            app.Run();
        }

        internal static string[] ResolveAllowedOrigins(IConfiguration configuration)
        {
            return configuration["CORS_ALLOWED_ORIGINS"]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? new[]
                {
                    "http://localhost:3000",
                    "http://127.0.0.1:3000",
                    "https://localhost",
                    "https://127.0.0.1"
                };
        }

        internal static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        }

        internal static void ConfigureApplicationCookie(CookieAuthenticationOptions options)
        {
            options.Cookie.Name = ".AspNetCore.Cookies";
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.EventsType = typeof(AuthCookieEvents);
        }

        internal static void ConfigureRateLimiter(RateLimiterOptions options)
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "global",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        }

        internal static int ResolveExceptionStatusCode(Exception? exception)
        {
            return exception switch
            {
                ForbiddenException => StatusCodes.Status403Forbidden,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                DomainValidationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
