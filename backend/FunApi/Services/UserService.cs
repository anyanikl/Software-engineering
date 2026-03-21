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
        private readonly FunDBcontext _context;

        public UserService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto> GetByIdAsync(int userId)
        {
            var user = await LoadUserProfileQuery()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                throw new KeyNotFoundException("Пользователь не найден");
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
                throw new KeyNotFoundException("Пользователь не найден");
            }

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(x => x.TargetUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.Author)
                .Select(x => new ReviewDto
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    AuthorId = x.AuthorId,
                    AuthorName = x.Author.FullName,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var activeAdvertisements = await _context.Advertisements
                .AsNoTracking()
                .Where(x => x.SellerId == userId && !x.IsDeleted && !x.IsArchived)
                .Include(x => x.Seller)
                .Include(x => x.Images)
                .Include(x => x.AdvertisementStatus)
                .Where(x => x.AdvertisementStatus.Name == "Активно" || x.AdvertisementStatus.Name == "Active")
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdvertisementCardDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    ShortDescription = x.Description.Length > 120
                        ? x.Description.Substring(0, 120) + "..."
                        : x.Description,
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
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                throw new KeyNotFoundException("Пользователь не найден");
            }

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(x =>
                    x.UniversityId == user.UniversityId &&
                    x.Name.ToLower() == dto.Faculty.Trim().ToLower());

            if (faculty is null)
            {
                throw new InvalidOperationException("Факультет не найден для текущего университета");
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.Phone.Trim();
            user.FacultyId = faculty.Id;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(userId);
        }

        public Task<string> UpdateAvatarAsync(int userId, IFormFile file)
        {
            throw new NotSupportedException("Загрузка аватара пока не реализована");
        }

        private IQueryable<UserProfileDto> LoadUserProfileQuery()
        {
            return _context.Users
                .AsNoTracking()
                .Include(x => x.Faculty)
                .Select(x => new UserProfileDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Phone = x.PhoneNumber,
                    AvatarUrl = x.AvatarUrl,
                    Faculty = x.Faculty.Name,
                    Rating = decimal.ToDouble(x.Rating),
                    ReviewsCount = x.ReviewsCount,
                    SalesCount = x.SellerOrders.Count(),
                    PurchasesCount = x.BuyerOrders.Count(),
                    ActiveAdvertisementsCount = x.Advertisements.Count(a => !a.IsDeleted && !a.IsArchived)
                });
        }
    }
}
