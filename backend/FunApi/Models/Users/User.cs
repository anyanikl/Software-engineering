using FunApi.Models.Advertisements;
using FunApi.Models.Auth;
using FunApi.Models.Chats;
using FunApi.Models.Favorites;
using FunApi.Models.Notifications;
using FunApi.Models.Orders;

namespace FunApi.Models.Users
{
    public class User
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public int UniversityId { get; set; }
        public int FacultyId { get; set; }

        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }

        public bool IsBlocked { get; set; }
        public bool IsEmailConfirmed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Role Role { get; set; } = null!;
        public University University { get; set; } = null!;
        public Faculty Faculty { get; set; } = null!;

        public Carts.Cart? Cart { get; set; }

        public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        public ICollection<Order> BuyerOrders { get; set; } = new List<Order>();
        public ICollection<Order> SellerOrders { get; set; } = new List<Order>();

        public ICollection<Review> WrittenReviews { get; set; } = new List<Review>();
        public ICollection<Review> ReceivedReviews { get; set; } = new List<Review>();

        public ICollection<Chat> BuyerChats { get; set; } = new List<Chat>();
        public ICollection<Chat> SellerChats { get; set; } = new List<Chat>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<AdvertisementModeration> ModerationActions { get; set; } = new List<AdvertisementModeration>();
    }
}
