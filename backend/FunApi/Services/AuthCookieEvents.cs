using System.Security.Claims;
using FunApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AuthCookieEvents : CookieAuthenticationEvents
    {
        private readonly FunDBcontext _context;

        public AuthCookieEvents(FunDBcontext context)
        {
            _context = context;
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            var userIdClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("Id");

            if (!int.TryParse(userIdClaim, out var userId))
            {
                await RejectPrincipalAsync(context);
                return;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null || user.IsBlocked || !user.IsEmailConfirmed)
            {
                await RejectPrincipalAsync(context);
                return;
            }

            var currentRole = principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            if (!string.Equals(currentRole, user.Role.Name, StringComparison.OrdinalIgnoreCase))
            {
                await RejectPrincipalAsync(context);
            }
        }

        private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
