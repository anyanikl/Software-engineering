namespace FunApi.Models.Users
{
    public class University
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Domain { get; set; } = null!;

        public ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
