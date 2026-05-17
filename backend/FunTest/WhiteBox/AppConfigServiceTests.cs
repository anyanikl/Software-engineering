using FunApi.Services;
using FunTest.TestHelpers;

namespace FunTest.WhiteBox
{
    public class AppConfigServiceTests
    {
        [Fact]
        public async Task GetAsync_ReturnsUniversitiesFacultiesDomainsAndPasswordPolicy()
        {
            await using var context = TestDbContextFactory.Create();
            var firstUniversity = await TestData.AddUniversityAsync(context, "Beta University", "beta.edu");
            await TestData.AddFacultyAsync(context, firstUniversity, "Math");
            var secondUniversity = await TestData.AddUniversityAsync(context, "Alpha University", "alpha.edu");
            await TestData.AddFacultyAsync(context, secondUniversity, "Physics");
            await TestData.AddFacultyAsync(context, secondUniversity, "Math");
            var service = new AppConfigService(context);

            var result = await service.GetAsync();

            Assert.Equal(["Alpha University", "Beta University"], result.Universities);
            Assert.Equal(["Math", "Physics"], result.Faculties);
            Assert.Contains(".edu", result.UniversityDomains);
            Assert.Contains(".ac.ru", result.UniversityDomains);
            Assert.Contains("alpha.edu", result.UniversityDomains);
            Assert.Equal(8, result.PasswordMinLength);
        }
    }
}
