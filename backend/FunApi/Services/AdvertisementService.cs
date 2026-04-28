using System.Linq.Expressions;
using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Advertisements;
using FunApi.Security;
using FunDto.Models.Contracts.Advertisements;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AdvertisementService : IAdvertisementService
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        private readonly FunDBcontext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAccessControlService _accessControlService;

        public AdvertisementService(
            FunDBcontext context,
            IWebHostEnvironment environment,
            IAccessControlService accessControlService)
        {
            _context = context;
            _environment = environment;
            _accessControlService = accessControlService;
        }

        public async Task<AdvertisementDto> GetByIdAsync(int advertisementId, int? viewerId = null)
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

            var isOwner = viewerId.HasValue && advertisement.SellerId == viewerId.Value;
            var hasElevatedAccess = viewerId.HasValue
                && await _accessControlService.HasAnyRoleAsync(viewerId.Value, AppRoles.Admin, AppRoles.Moderator);
            var isPubliclyVisible = IsPubliclyVisible(advertisement);

            if (!isPubliclyVisible && !isOwner && !hasElevatedAccess)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            var canSeeModerationData = isOwner || hasElevatedAccess;
            var mainImageUrl = advertisement.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Id)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();

            return new AdvertisementDto
            {
                Id = advertisement.Id,
                SellerId = advertisement.SellerId,
                Title = advertisement.Title,
                Description = advertisement.Description,
                Course = advertisement.Course,
                Type = advertisement.Type,
                Price = advertisement.Price,
                Location = advertisement.Location,
                SellerName = advertisement.Seller.FullName,
                Status = GetDisplayStatus(advertisement),
                ModeratorComment = canSeeModerationData ? advertisement.ModeratorComment : null,
                MainImageUrl = mainImageUrl,
                CanEdit = isOwner,
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
                CategoryId = await EnsureCategoryAsync(NormalizeRequired(dto.Type, "Type")),
                AdvertisementStatusId = await EnsureAdvertisementStatusAsync("pending"),
                Title = NormalizeRequired(dto.Title, "Title"),
                Description = NormalizeRequired(dto.Description, "Description"),
                Course = dto.Course,
                Type = NormalizeRequired(dto.Type, "Type"),
                Price = dto.Price,
                Location = NormalizeRequired(dto.Location, "Location"),
                IsArchived = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Advertisements.Add(advertisement);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(advertisement.Id, sellerId);
        }

        public async Task<AdvertisementDto> UpdateAsync(int sellerId, int advertisementId, UpdateAdvertisementDto dto)
        {
            var advertisement = await GetSellerAdvertisementAsync(sellerId, advertisementId);

            advertisement.Title = NormalizeRequired(dto.Title, "Title");
            advertisement.Description = NormalizeRequired(dto.Description, "Description");
            advertisement.Course = dto.Course;
            advertisement.Type = NormalizeRequired(dto.Type, "Type");
            advertisement.Price = dto.Price;
            advertisement.Location = NormalizeRequired(dto.Location, "Location");
            advertisement.CategoryId = await EnsureCategoryAsync(advertisement.Type);
            advertisement.AdvertisementStatusId = await EnsureAdvertisementStatusAsync("pending");
            advertisement.ModeratorComment = null;
            advertisement.IsArchived = false;
            advertisement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(advertisement.Id, sellerId);
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

        public async Task RestoreAsync(int sellerId, int advertisementId)
        {
            var advertisement = await GetSellerAdvertisementAsync(sellerId, advertisementId);
            advertisement.IsArchived = false;
            advertisement.AdvertisementStatusId = await EnsureAdvertisementStatusAsync("pending");
            advertisement.ModeratorComment = null;
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
                var normalizedStatus = status.Trim().ToLowerInvariant();
                query = normalizedStatus switch
                {
                    "archived" => query.Where(x => x.IsArchived),
                    _ => query.Where(x => !x.IsArchived && x.AdvertisementStatus.Name == normalizedStatus)
                };
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
                    Status = x.IsArchived ? "archived" : x.AdvertisementStatus.Name,
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

        public async Task<List<string>> AddImagesAsync(int sellerId, int advertisementId, List<IFormFile> files)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && x.SellerId == sellerId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            var validFiles = files.Where(x => x.Length > 0).ToList();
            if (validFiles.Count == 0)
            {
                throw new DomainValidationException("Image file is empty");
            }

            var uploadDirectory = Path.Combine(GetWebRootPath(), "uploads", "advertisements", advertisementId.ToString());
            Directory.CreateDirectory(uploadDirectory);

            var imageUrls = new List<string>();
            var makePrimary = !advertisement.Images.Any();
            foreach (var file in validFiles)
            {
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainValidationException("Only image files are allowed");
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedImageExtensions.Contains(extension))
                {
                    throw new DomainValidationException("Unsupported image extension");
                }

                var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var filePath = Path.Combine(uploadDirectory, fileName);

                await using (var stream = File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/advertisements/{advertisementId}/{fileName}";
                advertisement.Images.Add(new AdvertisementImage
                {
                    ImageUrl = imageUrl,
                    IsPrimary = makePrimary
                });
                imageUrls.Add(imageUrl);
                makePrimary = false;
            }

            advertisement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return imageUrls;
        }

        public async Task DeleteImagesAsync(int sellerId, int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && x.SellerId == sellerId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            foreach (var image in advertisement.Images)
            {
                DeletePhysicalImage(image.ImageUrl);
            }

            _context.AdvertisementImages.RemoveRange(advertisement.Images);
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

            DeletePhysicalImage(image.ImageUrl);
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
                SellerId = x.SellerId,
                Title = x.Title,
                ShortDescription = x.Description.Length > 120 ? x.Description.Substring(0, 120) + "..." : x.Description,
                Course = x.Course,
                Type = x.Type,
                Price = x.Price,
                Location = x.Location,
                SellerName = x.Seller.FullName,
                MainImageUrl = x.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                CreatedAt = x.CreatedAt,
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
            var normalizedName = name.Trim().ToLowerInvariant();
            var status = await _context.AdvertisementStatuses.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (status is not null) return status.Id;

            status = new AdvertisementStatus { Name = normalizedName };
            _context.AdvertisementStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }

        private async Task<int> EnsureCategoryAsync(string name)
        {
            var normalizedName = name.Trim();
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (category is not null) return category.Id;

            category = new Category { Name = normalizedName };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category.Id;
        }

        private static bool IsPubliclyVisible(Advertisement advertisement)
        {
            return !advertisement.IsArchived && advertisement.AdvertisementStatus.Name == "approved";
        }

        private static string GetDisplayStatus(Advertisement advertisement)
        {
            return advertisement.IsArchived ? "archived" : advertisement.AdvertisementStatus.Name;
        }

        private static string NormalizeRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException($"{fieldName} is required");
            }

            return value.Trim();
        }

        private string GetWebRootPath()
        {
            var webRootPath = _environment.WebRootPath;
            if (!string.IsNullOrWhiteSpace(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
                return webRootPath;
            }

            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRootPath);
            return webRootPath;
        }

        private void DeletePhysicalImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(GetWebRootPath(), relativePath));
            var uploadsRoot = Path.GetFullPath(Path.Combine(GetWebRootPath(), "uploads"));

            if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                return;
            }

            File.Delete(fullPath);
        }
    }
}
