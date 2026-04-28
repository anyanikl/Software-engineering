using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using FunDto.Models.Contracts.Advertisements;
using FunDto.Models.Contracts.Reviews;
using FunDto.Models.Contracts.Users;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class UserService : IUserService
    {
        private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

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

        public UserService(FunDBcontext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<UserProfileDto> GetByIdAsync(int userId)
        {
            var user = await LoadUserProfileQuery()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return user;
        }

        public Task<UserProfileDto> GetMyProfileAsync(int userId)
        {
            return GetByIdAsync(userId);
        }

        public async Task<PublicUserProfileDto> GetPublicProfileAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Faculty)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(x => x.TargetUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.Author)
                .Include(x => x.Order)
                .ThenInclude(x => x.Advertisement)
                .Select(x => new ReviewDto
                {
                    Id = x.Id,
                    AuthorName = x.Author.FullName,
                    ProductName = x.Order.Advertisement.Title,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var activeAdvertisements = await _context.Advertisements
                .AsNoTracking()
                .Where(x =>
                    x.SellerId == userId
                    && !x.IsDeleted
                    && !x.IsArchived
                    && x.AdvertisementStatus.Name == "approved")
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdvertisementCardDto
                {
                    Id = x.Id,
                    SellerId = x.SellerId,
                    Title = x.Title,
                    ShortDescription = x.Description.Length > 120
                        ? x.Description.Substring(0, 120) + "..."
                        : x.Description,
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
                })
                .ToListAsync();

            return new PublicUserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Faculty = user.Faculty.Name,
                Rating = decimal.ToDouble(user.Rating),
                ReviewsCount = user.ReviewsCount,
                Reviews = reviews,
                ActiveAdvertisements = activeAdvertisements
            };
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto dto)
        {
            var user = await _context.Users
                .Include(x => x.Faculty)
                .Include(x => x.University)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var facultyName = NormalizeRequired(dto.Faculty, "Faculty");
            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(x =>
                    x.UniversityId == user.UniversityId &&
                    x.Name.ToLower() == facultyName.ToLower());

            if (faculty is null)
            {
                throw new DomainValidationException("Faculty was not found for the current university");
            }

            user.FullName = NormalizeRequired(dto.FullName, "Full name");
            user.PhoneNumber = NormalizeRequired(dto.Phone, "Phone");
            user.FacultyId = faculty.Id;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(userId);
        }

        public async Task<string> UpdateAvatarAsync(int userId, IFormFile file)
        {
            if (file.Length == 0)
            {
                throw new DomainValidationException("Avatar file is empty");
            }

            if (file.Length > MaxAvatarSizeBytes)
            {
                throw new DomainValidationException("Avatar file must not exceed 5 MB");
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("Only image files are allowed");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new DomainValidationException("Unsupported avatar image format");
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            DeletePhysicalImage(user.AvatarUrl);

            var avatarDirectory = Path.Combine(GetWebRootPath(), "uploads", "avatars", userId.ToString());
            Directory.CreateDirectory(avatarDirectory);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(avatarDirectory, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/uploads/avatars/{userId}/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user.AvatarUrl;
        }

        public async Task DeleteAvatarAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                throw new KeyNotFoundException("User not found");
            }

            DeletePhysicalImage(user.AvatarUrl);
            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private IQueryable<UserProfileDto> LoadUserProfileQuery()
        {
            return _context.Users
                .AsNoTracking()
                .Include(x => x.Faculty)
                .Include(x => x.University)
                .Select(x => new UserProfileDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Phone = x.PhoneNumber,
                    AvatarUrl = x.AvatarUrl,
                    University = x.University.Name,
                    Faculty = x.Faculty.Name,
                    Rating = decimal.ToDouble(x.Rating),
                    ReviewsCount = x.ReviewsCount,
                    SalesCount = x.SellerOrders.Count(),
                    PurchasesCount = x.BuyerOrders.Count(),
                    ActiveAdvertisementsCount = x.Advertisements.Count(a =>
                        !a.IsDeleted
                        && !a.IsArchived
                        && a.AdvertisementStatus.Name == "approved")
                });
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

        private void DeletePhysicalImage(string? imageUrl)
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
