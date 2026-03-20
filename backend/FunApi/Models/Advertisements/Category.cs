namespace FunApi.Models.Advertisements
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
    }
}
