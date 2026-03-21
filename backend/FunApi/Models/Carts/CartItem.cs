using FunApi.Models.Advertisements;

namespace FunApi.Models.Carts
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int AdvertisementId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Cart Cart { get; set; } = null!;
        public Advertisement Advertisement { get; set; } = null!;
    }
}
