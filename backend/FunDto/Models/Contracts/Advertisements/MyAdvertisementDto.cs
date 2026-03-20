namespace FunDto.Models.Contracts.Advertisements
{
    public class MyAdvertisementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string Location { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ModeratorComment { get; set; }
        public int OrdersCount { get; set; }
        public string? MainImageUrl { get; set; }
    }
}
