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
                .Where(x => !x.IsDeleted)
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .Select(x => new AdvertisementCardDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    ShortDescription = x.Description.Length > 120 ? x.Description.Substring(0, 120) + "..." : x.Description,
                    Type = x.Type,
                    Price = x.Price,
                    SellerName = x.Seller.FullName,
                    MainImageUrl = x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
                    IsFavorite = true,
                    IsInCart = false
                })
                .ToListAsync();
        }

        public async Task AddAsync(int userId, int advertisementId)
        {
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
