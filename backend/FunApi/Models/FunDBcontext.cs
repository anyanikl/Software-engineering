using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace FunApi.Models
{
    public class FunDBcontext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IConfiguration? _configuration;

        public FunDBcontext()
        {
        }

        public FunDBcontext(
            DbContextOptions<FunDBcontext> options,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public DbSet<User> Users => Set<User>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString =
                    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Connection string not found!");

                optionsBuilder.UseNpgsql(connectionString);
            }
        }


        public override int SaveChanges()
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = base.SaveChanges();
            OnAfterSaveChanges(auditEntries);
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();

            var auditEntries = new List<AuditEntry>();
            var userId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                {
                    var auditEntry = new AuditEntry(entry)
                    {
                        TableName = entry.Metadata.GetTableName() ?? "",
                        Action = entry.State.ToString(),
                        UserId = userId
                    };

                    foreach (var property in entry.Properties)
                    {
                        auditEntry.NewValues[property.Metadata.Name] =
                            property.CurrentValue ?? "null";
                    }

                    auditEntries.Add(auditEntry);
                }
            }

            return auditEntries;
        }

        private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (!auditEntries.Any())
                return;

            var ip = _httpContextAccessor?
                .HttpContext?
                .Connection?
                .RemoteIpAddress?
                .ToString() ?? "unknown";

            foreach (var auditEntry in auditEntries)
            {
                Console.WriteLine(
                    $"[AUDIT] Table: {auditEntry.TableName}, " +
                    $"Action: {auditEntry.Action}, " +
                    $"User: {auditEntry.UserId}, " +
                    $"IP: {ip}");
            }
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor?
                .HttpContext?
                .User?
                .FindFirstValue("Id") ?? "Unauthorized";
        }
    }


    public class AuditEntry
    {
        public string TableName { get; set; } = "";
        public string Action { get; set; } = "";
        public string UserId { get; set; } = "";
        public Dictionary<string, object> NewValues { get; set; } = new();

        public AuditEntry(EntityEntry entry)
        {
        }
    }
}