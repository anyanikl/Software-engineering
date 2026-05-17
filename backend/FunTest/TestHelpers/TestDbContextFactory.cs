using System.Runtime.CompilerServices;
using FunApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FunTest.TestHelpers
{
    internal static class TestDbContextFactory
    {
        public static FunDBcontext Create([CallerMemberName] string testName = "")
        {
            var options = new DbContextOptionsBuilder<FunDBcontext>()
                .UseInMemoryDatabase($"{testName}-{Guid.NewGuid():N}")
                .EnableSensitiveDataLogging()
                .Options;

            var configuration = new ConfigurationBuilder().Build();
            return new FunDBcontext(options, new HttpContextAccessor(), configuration);
        }
    }
}
