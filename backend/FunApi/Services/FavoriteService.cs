using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Favorites;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly FunDBcontext _context;

        public FavoriteService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<List<AdvertisementCardDto>> GetAllAsync(int userId)
        {
            return await _context.Favorites
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.Advertisement)
                .Where(x => !x.IsDeleted && !x.IsArchived && x.AdvertisementStatus.Name == "approved")
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdvertisementCardDto
                {
                    Id = x.Id,
                    SellerId = x.SellerId,
                    Title = x.Title,
                    ShortDescription = x.Description.Length > 120 ? x.Description.Substring(0, 120) + "..." : x.Description,
                    Course = x.Course,
                    Type = x.Type,
                    Price = x.Price,
                    Location = x.Location,
                    SellerName = x.Seller.FullName,
                    MainImageUrl = x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    IsFavorite = true,
                    IsInCart = false
                })
                .ToListAsync();
        }

        public async Task AddAsync(int userId, int advertisementId)
        {
            var advertisementExists = await _context.Advertisements.AnyAsync(x =>
                x.Id == advertisementId
                && !x.IsDeleted
                && !x.IsArchived
                && x.AdvertisementStatus.Name == "approved");

            if (!advertisementExists)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            if (await _context.Favorites.AnyAsync(x => x.UserId == userId && x.AdvertisementId == advertisementId))
            {
                return;
            }

            _context.Favorites.Add(new Favorite
            {
                UserId = userId,
                AdvertisementId = advertisementId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(int userId, int advertisementId)
        {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(x => x.UserId == userId && x.AdvertisementId == advertisementId);
            if (favorite is null) return;

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int userId, int advertisementId)
        {
            return _context.Favorites.AnyAsync(x => x.UserId == userId && x.AdvertisementId == advertisementId);
        }
    }
}
