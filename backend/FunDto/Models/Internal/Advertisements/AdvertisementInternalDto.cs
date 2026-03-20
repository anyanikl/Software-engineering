namespace FunDto.Models.Internal.Advertisements
{
    public class AdvertisementInternalDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Course { get; set; }
        public string Type { get; set; } = null!;
        public decimal Price { get; set; }
        public string Location { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ModeratorComment { get; set; }
    }
}
