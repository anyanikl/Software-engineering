namespace FunApi.Models.Orders
{
    public class OrderStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
