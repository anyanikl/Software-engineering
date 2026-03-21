using System.ComponentModel.DataAnnotations;

namespace FunDto.Models.Contracts.Users
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Faculty { get; set; } = null!;
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public int SalesCount { get; set; }
        public int PurchasesCount { get; set; }
        public int ActiveAdvertisementsCount { get; set; }
    }
}
