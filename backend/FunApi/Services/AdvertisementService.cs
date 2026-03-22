using System.Linq.Expressions;
using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Advertisements;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AdvertisementService : IAdvertisementService
    {
        private readonly FunDBcontext _context;

        public AdvertisementService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<AdvertisementDto> GetByIdAsync(int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .AsNoTracking()
                .Include(x => x.Seller)
                .Include(x => x.AdvertisementStatus)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            return new AdvertisementDto
            {
                Id = advertisement.Id,
                Title = advertisement.Title,
                Description = advertisement.Description,
                Course = advertisement.Course,
                Type = advertisement.Type,
                Price = advertisement.Price,
                Location = advertisement.Location,
                SellerName = advertisement.Seller.FullName,
                Status = advertisement.AdvertisementStatus.Name,
                ModeratorComment = advertisement.ModeratorComment,
                ImageUrls = advertisement.Images
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.Id)
                    .Select(x => x.ImageUrl)
                    .ToList()
            };
        }

        public async Task<List<AdvertisementCardDto>> GetAllAsync(AdvertisementFilterDto filter)
        {
            return await SearchAsync(filter);
        }

        public async Task<List<AdvertisementCardDto>> SearchAsync(AdvertisementFilterDto filter)
        {
            var query = ApplyFilter(BuildVisibleAdvertisementsQuery(), filter);
            return await query
                .Select(MapCard())
                .ToListAsync();
        }

        public async Task<AdvertisementDto> CreateAsync(int sellerId, CreateAdvertisementDto dto)
        {
            var advertisement = new Advertisement
            {
                SellerId = sellerId,
                CategoryId = await EnsureCategoryAsync(dto.Type),
                AdvertisementStatusId = await EnsureAdvertisementStatusAsync("pending"),
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Course = dto.Course,
                Type = dto.Type.Trim(),
                Price = dto.Price,
                Location = dto.Location.Trim(),
                IsArchived = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Advertisements.Add(advertisement);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(advertisement.Id);
        }

        public async Task<AdvertisementDto> UpdateAsync(int sellerId, int advertisementId, UpdateAdvertisementDto dto)
        {
            var advertisement = await GetSellerAdvertisementAsync(sellerId, advertisementId);
            advertisement.Title = dto.Title.Trim();
            advertisement.Description = dto.Description.Trim();
            advertisement.Course = dto.Course;
            advertisement.Type = dto.Type.Trim();
            advertisement.Price = dto.Price;
            advertisement.Location = dto.Location.Trim();
            advertisement.CategoryId = await EnsureCategoryAsync(dto.Type);
            advertisement.AdvertisementStatusId = await EnsureAdvertisementStatusAsync("pending");
            advertisement.ModeratorComment = null;
            advertisement.IsArchived = false;
            advertisement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(advertisement.Id);
        }

        public async Task DeleteAsync(int sellerId, int advertisementId)
        {
            var advertisement = await GetSellerAdvertisementAsync(sellerId, advertisementId);
            advertisement.IsDeleted = true;
            advertisement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ArchiveAsync(int sellerId, int advertisementId)
        {
            var advertisement = await GetSellerAdvertisementAsync(sellerId, advertisementId);
            advertisement.IsArchived = true;
            advertisement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<List<MyAdvertisementDto>> GetMyAdvertisementsAsync(int sellerId, string? status = null)
        {
            var query = _context.Advertisements
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId && !x.IsDeleted)
                .Include(x => x.AdvertisementStatus)
                .Include(x => x.Images)
                .Include(x => x.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.AdvertisementStatus.Name == status);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new MyAdvertisementDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Price = x.Price,
                    Location = x.Location,
                    Status = x.AdvertisementStatus.Name,
                    ModeratorComment = x.ModeratorComment,
                    OrdersCount = x.Orders.Count,
                    MainImageUrl = x.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.Id)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task AddImagesAsync(int sellerId, int advertisementId, List<IFormFile> files)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && x.SellerId == sellerId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            var makePrimary = !advertisement.Images.Any();
            foreach (var file in files)
            {
                advertisement.Images.Add(new AdvertisementImage
                {
                    ImageUrl = $"/uploads/advertisements/{advertisementId}/{Guid.NewGuid():N}-{file.FileName}",
                    IsPrimary = makePrimary
                });
                makePrimary = false;
            }

            advertisement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageAsync(int sellerId, int advertisementId, int imageId)
        {
            var image = await _context.AdvertisementImages
                .Include(x => x.Advertisement)
                .FirstOrDefaultAsync(x => x.Id == imageId
                    && x.AdvertisementId == advertisementId
                    && x.Advertisement.SellerId == sellerId);

            if (image is null)
            {
                throw new KeyNotFoundException("Image not found");
            }

            _context.AdvertisementImages.Remove(image);
            await _context.SaveChangesAsync();
        }

        private IQueryable<Advertisement> BuildVisibleAdvertisementsQuery()
        {
            return _context.Advertisements
                .AsNoTracking()
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .Include(x => x.AdvertisementStatus)
                .Where(x => !x.IsDeleted && !x.IsArchived && x.AdvertisementStatus.Name == "approved");
        }

        private IQueryable<Advertisement> ApplyFilter(IQueryable<Advertisement> query, AdvertisementFilterDto filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(search) || x.Description.ToLower().Contains(search));
            }

            if (filter.Course.HasValue)
            {
                query = query.Where(x => x.Course == filter.Course.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                var type = filter.Type.Trim().ToLower();
                query = query.Where(x => x.Type.ToLower() == type);
            }

            return filter.SortBy switch
            {
                "price_asc" => query.OrderBy(x => x.Price),
                "price_desc" => query.OrderByDescending(x => x.Price),
                "date_asc" => query.OrderBy(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };
        }

        private static Expression<Func<Advertisement, AdvertisementCardDto>> MapCard()
        {
            return x => new AdvertisementCardDto
            {
                Id = x.Id,
                Title = x.Title,
                ShortDescription = x.Description.Length > 120 ? x.Description.Substring(0, 120) + "..." : x.Description,
                Type = x.Type,
                Price = x.Price,
                SellerName = x.Seller.FullName,
                MainImageUrl = x.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                IsFavorite = false,
                IsInCart = false
            };
        }

        private async Task<Advertisement> GetSellerAdvertisementAsync(int sellerId, int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .FirstOrDefaultAsync(x => x.Id == advertisementId && x.SellerId == sellerId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            return advertisement;
        }

        private async Task<int> EnsureAdvertisementStatusAsync(string name)
        {
            var status = await _context.AdvertisementStatuses.FirstOrDefaultAsync(x => x.Name == name);
            if (status is not null) return status.Id;

            status = new AdvertisementStatus { Name = name };
            _context.AdvertisementStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }

        private async Task<int> EnsureCategoryAsync(string name)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.Name == name);
            if (category is not null) return category.Id;

            category = new Category { Name = name };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category.Id;
        }
    }
}
