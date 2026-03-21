namespace FunApi.Models.Users
{
    public class Faculty
    {
        public int Id { get; set; }
        public int UniversityId { get; set; }
        public string Name { get; set; } = null!;

        public University University { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
