namespace FunDto.Models.Contracts.Advertisements
{
    public class AdvertisementCardDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string Title { get; set; } = null!;
        public string ShortDescription { get; set; } = null!;
        public int Course { get; set; }
        public string Type { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Location { get; set; }
        public string SellerName { get; set; } = null!;
        public string? MainImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsInCart { get; set; }
    }
}
