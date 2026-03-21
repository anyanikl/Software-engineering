namespace FunDto.Models.Contracts.Advertisements
{
    public class ModerationAdvertisementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string Location { get; set; } = null!;
        public string SellerName { get; set; } = null!;
        public List<string> ImageUrls { get; set; } = new();
    }
}
