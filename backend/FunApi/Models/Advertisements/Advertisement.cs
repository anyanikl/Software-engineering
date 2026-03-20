using FunApi.Models.Carts;
using FunApi.Models.Chats;
using FunApi.Models.Favorites;
using FunApi.Models.Orders;
using FunApi.Models.Users;

namespace FunApi.Models.Advertisements
{
    public class Advertisement
    {
        public int Id { get; set; }

        public int SellerId { get; set; }
        public int CategoryId { get; set; }
        public int AdvertisementStatusId { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Course { get; set; }
        public string Type { get; set; } = null!;
        public decimal Price { get; set; }
        public string Location { get; set; } = null!;
        public string? ModeratorComment { get; set; }

        public bool IsArchived { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User Seller { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public AdvertisementStatus AdvertisementStatus { get; set; } = null!;

        public ICollection<AdvertisementImage> Images { get; set; } = new List<AdvertisementImage>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
        public ICollection<AdvertisementModeration> ModerationHistory { get; set; } = new List<AdvertisementModeration>();
    }
}
