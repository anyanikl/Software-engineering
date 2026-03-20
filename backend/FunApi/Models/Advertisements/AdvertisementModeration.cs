using FunApi.Models.Users;

namespace FunApi.Models.Advertisements
{
    public class AdvertisementModeration
    {
        public int Id { get; set; }

        public int AdvertisementId { get; set; }
        public int ModeratorId { get; set; }

        public string Decision { get; set; } = null!;
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public Advertisement Advertisement { get; set; } = null!;
        public User Moderator { get; set; } = null!;
    }
}
