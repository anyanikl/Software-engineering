using FunDto.Models.Contracts.Advertisements;

namespace FunDto.Models.Contracts.Cart
{
    public class CartDto
    {
        public List<AdvertisementCardDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
