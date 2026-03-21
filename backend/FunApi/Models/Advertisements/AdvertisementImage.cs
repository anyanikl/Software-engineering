namespace FunApi.Models.Advertisements
{
    public class AdvertisementImage
    {
        public int Id { get; set; }
        public int AdvertisementId { get; set; }

        public string ImageUrl { get; set; } = null!;
        public bool IsPrimary { get; set; }

        public Advertisement Advertisement { get; set; } = null!;
    }
}
