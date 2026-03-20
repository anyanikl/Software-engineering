namespace FunDto.Models.Contracts.Admin
{
    public class UserAdminDto
    {
        public int TotalUsers { get; set; }
        public int ActiveAdvertisements { get; set; }
        public int CompletedOrders { get; set; }
        public int BlockedUsers { get; set; }
    }
}
