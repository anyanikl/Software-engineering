using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace FunApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var isDevelopment = builder.Environment.IsDevelopment();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<FunDBcontext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".AspNetCore.Cookies";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = isDevelopment
                        ? CookieSecurePolicy.None
                        : CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                    options.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                });
            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "__Host-antiforgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            builder.Services.AddRateLimiter(options =>
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
            });

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

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

            app.UseRateLimiter();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Server error",
                        message = "Произошла ошибка"
                    });
                });
            });

            if (!isDevelopment)
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
