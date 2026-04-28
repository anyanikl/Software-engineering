namespace FunDto.Models.Contracts.Config
{
    public class AppConfigDto
    {
        public List<string> UniversityDomains { get; set; } = new();
        public List<string> Universities { get; set; } = new();
        public List<string> Faculties { get; set; } = new();
        public int PasswordMinLength { get; set; }
    }
}
