using FunApi.Models.Advertisements;
using FunApi.Models.Users;

namespace FunApi.Models.Favorites
{
    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AdvertisementId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public Advertisement Advertisement { get; set; } = null!;
    }
}
