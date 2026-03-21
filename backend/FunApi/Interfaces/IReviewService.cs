using FunDto.Models.Contracts.Reviews;

namespace FunApi.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateAsync(int authorId, CreateReviewDto dto);
        Task<List<ReviewDto>> GetByUserIdAsync(int userId);
        Task<bool> CanLeaveReviewAsync(int authorId, int orderId);
    }
}
