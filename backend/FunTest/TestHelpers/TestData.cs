using FunApi.Models;
using FunApi.Models.Advertisements;
using FunApi.Models.Auth;
using FunApi.Models.Carts;
using FunApi.Models.Chats;
using FunApi.Models.Favorites;
using FunApi.Models.Notifications;
using FunApi.Models.Orders;
using FunApi.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace FunTest.TestHelpers
{
    internal static class TestData
    {
        public static async Task<Role> AddRoleAsync(FunDBcontext context, string name)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            var role = await context.Roles.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (role is not null)
            {
                return role;
            }

            role = new Role { Name = normalizedName };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
            return role;
        }

        public static async Task<University> AddUniversityAsync(FunDBcontext context, string name = "Test University", string domain = "test.edu")
        {
            var university = await context.Universities.FirstOrDefaultAsync(x => x.Name == name);
            if (university is not null)
            {
                return university;
            }

            university = new University { Name = name, Domain = domain };
            context.Universities.Add(university);
            await context.SaveChangesAsync();
            return university;
        }

        public static async Task<Faculty> AddFacultyAsync(FunDBcontext context, University university, string name = "Engineering")
        {
            var faculty = await context.Faculties.FirstOrDefaultAsync(x => x.UniversityId == university.Id && x.Name == name);
            if (faculty is not null)
            {
                return faculty;
            }

            faculty = new Faculty { UniversityId = university.Id, Name = name };
            context.Faculties.Add(faculty);
            await context.SaveChangesAsync();
            return faculty;
        }

        public static async Task<User> AddUserAsync(
            FunDBcontext context,
            string email,
            string fullName,
            string roleName = "user",
            bool isBlocked = false,
            bool isEmailConfirmed = true)
        {
            var role = await AddRoleAsync(context, roleName);
            var university = await AddUniversityAsync(context);
            var faculty = await AddFacultyAsync(context, university);
            var user = new User
            {
                RoleId = role.Id,
                UniversityId = university.Id,
                FacultyId = faculty.Id,
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = "hashed-password",
                FullName = fullName,
                PhoneNumber = "+79990000000",
                Rating = 0,
                ReviewsCount = 0,
                IsBlocked = isBlocked,
                IsEmailConfirmed = isEmailConfirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public static async Task<Category> AddCategoryAsync(FunDBcontext context, string name = "Books")
        {
            var category = await context.Categories.FirstOrDefaultAsync(x => x.Name == name);
            if (category is not null)
            {
                return category;
            }

            category = new Category { Name = name };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        public static async Task<AdvertisementStatus> AddAdvertisementStatusAsync(FunDBcontext context, string name)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            var status = await context.AdvertisementStatuses.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (status is not null)
            {
                return status;
            }

            status = new AdvertisementStatus { Name = normalizedName };
            context.AdvertisementStatuses.Add(status);
            await context.SaveChangesAsync();
            return status;
        }

        public static async Task<Advertisement> AddAdvertisementAsync(
            FunDBcontext context,
            User seller,
            string statusName = "approved",
            string title = "Linear Algebra Notes",
            decimal price = 100,
            bool isArchived = false,
            bool isDeleted = false,
            DateTime? createdAt = null)
        {
            var category = await AddCategoryAsync(context);
            var status = await AddAdvertisementStatusAsync(context, statusName);
            var advertisement = new Advertisement
            {
                SellerId = seller.Id,
                CategoryId = category.Id,
                AdvertisementStatusId = status.Id,
                Title = title,
                Description = $"{title} description",
                Course = 2,
                Type = category.Name,
                Price = price,
                Location = "Campus",
                ModeratorComment = statusName == "rejected" ? "Needs fixes" : null,
                IsArchived = isArchived,
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Advertisements.Add(advertisement);
            await context.SaveChangesAsync();
            return advertisement;
        }

        public static async Task<AdvertisementImage> AddAdvertisementImageAsync(
            FunDBcontext context,
            Advertisement advertisement,
            string url = "/uploads/advertisements/1/image.jpg",
            bool isPrimary = true)
        {
            var image = new AdvertisementImage
            {
                AdvertisementId = advertisement.Id,
                ImageUrl = url,
                IsPrimary = isPrimary
            };

            context.AdvertisementImages.Add(image);
            await context.SaveChangesAsync();
            return image;
        }

        public static async Task<Cart> AddCartAsync(FunDBcontext context, User user)
        {
            var cart = await context.Carts.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (cart is not null)
            {
                return cart;
            }

            cart = new Cart
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Carts.Add(cart);
            await context.SaveChangesAsync();
            return cart;
        }

        public static async Task AddCartItemAsync(FunDBcontext context, Cart cart, Advertisement advertisement)
        {
            context.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                AdvertisementId = advertisement.Id,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        public static async Task AddFavoriteAsync(FunDBcontext context, User user, Advertisement advertisement)
        {
            context.Favorites.Add(new Favorite
            {
                UserId = user.Id,
                AdvertisementId = advertisement.Id,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        public static async Task<OrderStatus> AddOrderStatusAsync(FunDBcontext context, string name)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            var status = await context.OrderStatuses.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (status is not null)
            {
                return status;
            }

            status = new OrderStatus { Name = normalizedName };
            context.OrderStatuses.Add(status);
            await context.SaveChangesAsync();
            return status;
        }

        public static async Task<Order> AddOrderAsync(
            FunDBcontext context,
            User buyer,
            User seller,
            Advertisement advertisement,
            string statusName = "pending",
            decimal? price = null)
        {
            var status = await AddOrderStatusAsync(context, statusName);
            var order = new Order
            {
                AdvertisementId = advertisement.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                OrderStatusId = status.Id,
                Price = price ?? advertisement.Price,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = statusName == "completed" ? DateTime.UtcNow : null
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        public static async Task<Review> AddReviewAsync(
            FunDBcontext context,
            Order order,
            User author,
            User targetUser,
            int rating = 5,
            string comment = "Great deal")
        {
            var review = new Review
            {
                OrderId = order.Id,
                AuthorId = author.Id,
                TargetUserId = targetUser.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            context.Reviews.Add(review);
            await context.SaveChangesAsync();
            return review;
        }

        public static async Task<Chat> AddChatAsync(FunDBcontext context, Advertisement advertisement, User buyer, User seller)
        {
            var chat = new Chat
            {
                AdvertisementId = advertisement.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Chats.Add(chat);
            await context.SaveChangesAsync();
            return chat;
        }

        public static async Task<Message> AddMessageAsync(
            FunDBcontext context,
            Chat chat,
            User sender,
            string content = "Hello",
            bool isRead = false)
        {
            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = sender.Id,
                Content = content,
                IsRead = isRead,
                CreatedAt = DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();
            return message;
        }

        public static async Task<NotificationType> AddNotificationTypeAsync(FunDBcontext context, string name)
        {
            var type = await context.NotificationTypes.FirstOrDefaultAsync(x => x.Name == name);
            if (type is not null)
            {
                return type;
            }

            type = new NotificationType { Name = name };
            context.NotificationTypes.Add(type);
            await context.SaveChangesAsync();
            return type;
        }
    }
}
