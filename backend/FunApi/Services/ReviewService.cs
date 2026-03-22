using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Orders;
using FunDto.Models.Contracts.Reviews;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class ReviewService : IReviewService
    {
        private readonly FunDBcontext _context;

        public ReviewService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<ReviewDto> CreateAsync(int authorId, CreateReviewDto dto)
        {
            if (!await CanLeaveReviewAsync(authorId, dto.OrderId))
            {
                throw new InvalidOperationException("Review is not allowed");
            }

            var order = await _context.Orders
                .Include(x => x.Buyer)
                .Include(x => x.Seller)
                .FirstAsync(x => x.Id == dto.OrderId);

            var targetUserId = order.BuyerId == authorId ? order.SellerId : order.BuyerId;
            var review = new Review
            {
                OrderId = dto.OrderId,
                AuthorId = authorId,
                TargetUserId = targetUserId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            await UpdateUserRatingAsync(targetUserId);

            return await _context.Reviews
                .AsNoTracking()
                .Where(x => x.Id == review.Id)
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
                .FirstAsync();
        }

        public async Task<List<ReviewDto>> GetByUserIdAsync(int userId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Where(x => x.TargetUserId == userId)
                .Include(x => x.Author)
                .OrderByDescending(x => x.CreatedAt)
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
        }

        public async Task<bool> CanLeaveReviewAsync(int authorId, int orderId)
        {
            var order = await _context.Orders
                .Include(x => x.OrderStatus)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null || order.OrderStatus.Name != "completed")
            {
                return false;
            }

            if (order.BuyerId != authorId && order.SellerId != authorId)
            {
                return false;
            }

            return !await _context.Reviews.AnyAsync(x => x.OrderId == orderId && x.AuthorId == authorId);
        }

        private async Task UpdateUserRatingAsync(int userId)
        {
            var reviews = await _context.Reviews.Where(x => x.TargetUserId == userId).ToListAsync();
            var user = await _context.Users.FirstAsync(x => x.Id == userId);
            user.ReviewsCount = reviews.Count;
            user.Rating = reviews.Count == 0 ? 0 : (decimal)reviews.Average(x => x.Rating);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
