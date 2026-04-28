using FunDto.Models.Contracts.Users;

namespace FunApi.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetByIdAsync(int userId);
        Task<UserProfileDto> GetMyProfileAsync(int userId);
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto dto);
        Task<string> UpdateAvatarAsync(int userId, IFormFile file);
        Task DeleteAvatarAsync(int userId);
        Task<PublicUserProfileDto> GetPublicProfileAsync(int userId);
    }
}
