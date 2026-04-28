using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Carts;
using FunDto.Models.Contracts.Cart;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class CartService : ICartService
    {
        private readonly FunDBcontext _context;

        public CartService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            return await _context.Carts
                .AsNoTracking()
                .Where(x => x.Id == cart.Id)
                .Select(x => new CartDto
                {
                    Items = x.Items
                        .Where(i => !i.Advertisement.IsDeleted && !i.Advertisement.IsArchived && i.Advertisement.AdvertisementStatus.Name == "approved")
                        .Select(i => new FunDto.Models.Contracts.Advertisements.AdvertisementCardDto
                        {
                            Id = i.Advertisement.Id,
                            SellerId = i.Advertisement.SellerId,
                            Title = i.Advertisement.Title,
                            ShortDescription = i.Advertisement.Description.Length > 120 ? i.Advertisement.Description.Substring(0, 120) + "..." : i.Advertisement.Description,
                            Course = i.Advertisement.Course,
                            Type = i.Advertisement.Type,
                            Price = i.Advertisement.Price,
                            Location = i.Advertisement.Location,
                            SellerName = i.Advertisement.Seller.FullName,
                            MainImageUrl = i.Advertisement.Images.OrderByDescending(img => img.IsPrimary).ThenBy(img => img.Id).Select(img => img.ImageUrl).FirstOrDefault(),
                            CreatedAt = i.Advertisement.CreatedAt,
                            IsFavorite = false,
                            IsInCart = true
                        }).ToList(),
                    TotalCount = x.Items.Count(i => !i.Advertisement.IsDeleted && !i.Advertisement.IsArchived && i.Advertisement.AdvertisementStatus.Name == "approved"),
                    TotalPrice = x.Items
                        .Where(i => !i.Advertisement.IsDeleted && !i.Advertisement.IsArchived && i.Advertisement.AdvertisementStatus.Name == "approved")
                        .Sum(i => i.Advertisement.Price)
                })
                .FirstAsync();
        }

        public async Task AddItemAsync(int userId, int advertisementId)
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

            var cart = await GetOrCreateCartAsync(userId);
            if (await _context.CartItems.AnyAsync(x => x.CartId == cart.Id && x.AdvertisementId == advertisementId))
            {
                return;
            }

            _context.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                AdvertisementId = advertisementId,
                CreatedAt = DateTime.UtcNow
            });
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int userId, int advertisementId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == userId);
            if (cart is null) return;

            var item = await _context.CartItems.FirstOrDefaultAsync(x => x.CartId == cart.Id && x.AdvertisementId == advertisementId);
            if (item is null) return;

            _context.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ClearAsync(int userId)
        {
            var cart = await _context.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.UserId == userId);
            if (cart is null) return;

            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == userId);
            if (cart is not null) return cart;

            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }
    }
}
