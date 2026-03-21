using FunApi.Models.Advertisements;
using FunApi.Models.Auth;
using FunApi.Models.Carts;
using FunApi.Models.Chats;
using FunApi.Models.Favorites;
using FunApi.Models.Notifications;
using FunApi.Models.Orders;
using FunApi.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        // Users
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<University> Universities => Set<University>();
        public DbSet<Faculty> Faculties => Set<Faculty>();

        // Advertisements
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Advertisement> Advertisements => Set<Advertisement>();
        public DbSet<AdvertisementImage> AdvertisementImages => Set<AdvertisementImage>();
        public DbSet<AdvertisementStatus> AdvertisementStatuses => Set<AdvertisementStatus>();
        public DbSet<AdvertisementModeration> AdvertisementModerations => Set<AdvertisementModeration>();

        // Favorites
        public DbSet<Favorite> Favorites => Set<Favorite>();

        // Cart
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        // Orders
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
        public DbSet<Review> Reviews => Set<Review>();

        // Chats
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();

        // Notifications
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationType> NotificationTypes => Set<NotificationType>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany(u => u.BuyerOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Seller)
                .WithMany(u => u.SellerOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Buyer)
                .WithMany(u => u.BuyerChats)
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Seller)
                .WithMany(u => u.SellerChats)
                .HasForeignKey(c => c.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId);

            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.AdvertisementId })
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.AdvertisementId })
                .IsUnique();
        }

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