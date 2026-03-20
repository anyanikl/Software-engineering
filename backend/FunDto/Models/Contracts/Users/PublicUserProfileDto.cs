using FunDto.Models.Contracts.Advertisements;
using FunDto.Models.Contracts.Reviews;

namespace FunDto.Models.Contracts.Users
{
    public class PublicUserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Faculty { get; set; } = null!;
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();
        public List<AdvertisementCardDto> ActiveAdvertisements { get; set; } = new();
    }
}
