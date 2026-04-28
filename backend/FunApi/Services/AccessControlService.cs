using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly FunDBcontext _context;

        public AccessControlService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task EnsureAnyRoleAsync(int userId, params string[] roles)
        {
            if (!await HasAnyRoleAsync(userId, roles))
            {
                throw new ForbiddenException("Forbidden");
            }
        }

        public async Task<bool> HasAnyRoleAsync(int userId, params string[] roles)
        {
            if (roles.Length == 0)
            {
                return false;
            }

            var normalizedRoles = roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            if (normalizedRoles.Count == 0)
            {
                return false;
            }

            var userRole = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.Role.Name.ToLower())
                .FirstOrDefaultAsync();

            return userRole is not null && normalizedRoles.Contains(userRole);
        }
    }
}
