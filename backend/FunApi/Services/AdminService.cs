using System.Text;
using System.Text.Json;
using FunApi.Interfaces;
using FunApi.Models;
using FunDto.Models.Contracts.Admin;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AdminService : IAdminService
    {
        private readonly FunDBcontext _context;

        public AdminService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<List<AdminStatsDto>> GetUsersAsync(UserAdminFilterDto filter)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(x => x.FullName.ToLower().Contains(search) || x.Email.ToLower().Contains(search));
            }

            if (filter.IsBlocked.HasValue)
            {
                query = query.Where(x => x.IsBlocked == filter.IsBlocked.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminStatsDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Role = x.Role.Name,
                    CreatedAt = x.CreatedAt,
                    IsBlocked = x.IsBlocked
                })
                .ToListAsync();
        }

        public async Task BlockUserAsync(int adminId, int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null) throw new KeyNotFoundException("User not found");
            user.IsBlocked = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UnblockUserAsync(int adminId, int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null) throw new KeyNotFoundException("User not found");
            user.IsBlocked = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<UserAdminDto> GetStatsAsync()
        {
            return new UserAdminDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveAdvertisements = await _context.Advertisements.CountAsync(x => !x.IsDeleted && !x.IsArchived && x.AdvertisementStatus.Name == "approved"),
                CompletedOrders = await _context.Orders.CountAsync(x => x.OrderStatus.Name == "completed"),
                BlockedUsers = await _context.Users.CountAsync(x => x.IsBlocked)
            };
        }

        public async Task<string> ExportUsersCsvAsync()
        {
            var users = await GetUsersAsync(new UserAdminFilterDto());
            var builder = new StringBuilder();
            builder.AppendLine("Id,FullName,Email,Role,CreatedAt,IsBlocked");
            foreach (var user in users)
            {
                builder.AppendLine($"{user.Id},\"{user.FullName}\",\"{user.Email}\",\"{user.Role}\",{user.CreatedAt:O},{user.IsBlocked}");
            }

            return builder.ToString();
        }

        public async Task<string> ExportUsersJsonAsync()
        {
            var users = await GetUsersAsync(new UserAdminFilterDto());
            return JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
