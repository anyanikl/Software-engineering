using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Advertisements;
using FunApi.Exceptions;
using FunApi.Security;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class ModerationService : IModerationService
    {
        private readonly FunDBcontext _context;
        private readonly INotificationService _notificationService;
        private readonly IAccessControlService _accessControlService;

        public ModerationService(
            FunDBcontext context,
            INotificationService notificationService,
            IAccessControlService accessControlService)
        {
            _context = context;
            _notificationService = notificationService;
            _accessControlService = accessControlService;
        }

        public Task ApproveAsync(int moderatorId, int advertisementId, string? comment)
        {
            return ModerateAsync(moderatorId, advertisementId, "approved", comment);
        }

        public async Task<List<ModerationAdvertisementDto>> GetPendingAsync(int moderatorId)
        {
            await _accessControlService.EnsureAnyRoleAsync(moderatorId, AppRoles.Admin, AppRoles.Moderator);

            return await _context.Advertisements
                .AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsArchived && x.AdvertisementStatus.Name == "pending")
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new ModerationAdvertisementDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Price = x.Price,
                    Location = x.Location,
                    SellerName = x.Seller.FullName,
                    ImageUrls = x.Images.Select(i => i.ImageUrl).ToList()
                })
                .ToListAsync();
        }

        public Task RejectAsync(int moderatorId, int advertisementId, string comment)
        {
            return ModerateAsync(moderatorId, advertisementId, "rejected", comment);
        }

        public Task SendForRevisionAsync(int moderatorId, int advertisementId, string comment)
        {
            return ModerateAsync(moderatorId, advertisementId, "revision", comment);
        }

        private async Task ModerateAsync(int moderatorId, int advertisementId, string decision, string? comment)
        {
            await _accessControlService.EnsureAnyRoleAsync(moderatorId, AppRoles.Admin, AppRoles.Moderator);

            var advertisement = await _context.Advertisements
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);
            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            if (advertisement.SellerId == moderatorId)
            {
                throw new ForbiddenException("You cannot moderate your own advertisement");
            }

            advertisement.AdvertisementStatusId = await EnsureStatusAsync(decision);
            advertisement.ModeratorComment = comment;
            advertisement.UpdatedAt = DateTime.UtcNow;

            _context.AdvertisementModerations.Add(new AdvertisementModeration
            {
                AdvertisementId = advertisementId,
                ModeratorId = moderatorId,
                Decision = decision,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _notificationService.CreateAsync(
                advertisement.SellerId,
                "moderation",
                "Moderation update",
                $"{advertisement.Title}: {decision}");
        }

        private async Task<int> EnsureStatusAsync(string name)
        {
            var status = await _context.AdvertisementStatuses.FirstOrDefaultAsync(x => x.Name == name);
            if (status is not null) return status.Id;

            status = new AdvertisementStatus { Name = name };
            _context.AdvertisementStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }
    }
}
