using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Security;
using FunDto.Models.Contracts.Config;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class AppConfigService : IAppConfigService
    {
        private static readonly string[] DefaultUniversityDomains = [".edu", ".ac.ru"];

        private readonly FunDBcontext _context;

        public AppConfigService(FunDBcontext context)
        {
            _context = context;
        }

        public async Task<AppConfigDto> GetAsync(CancellationToken cancellationToken = default)
        {
            var universities = await _context.Universities
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);

            var faculties = await _context.Faculties
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            var universityDomains = await _context.Universities
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.Domain))
                .OrderBy(x => x.Domain)
                .Select(x => x.Domain!)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var defaultDomain in DefaultUniversityDomains)
            {
                if (!universityDomains.Contains(defaultDomain, StringComparer.OrdinalIgnoreCase))
                {
                    universityDomains.Add(defaultDomain);
                }
            }

            return new AppConfigDto
            {
                UniversityDomains = universityDomains.OrderBy(x => x).ToList(),
                Universities = universities,
                Faculties = faculties,
                PasswordMinLength = PasswordPolicy.MinLength
            };
        }
    }
}
