using FunApi.Exceptions;
using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Advertisements;
using FunApi.Models.Orders;
using FunDto.Models.Contracts.Orders;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly FunDBcontext _context;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;

        public OrderService(FunDBcontext context, ICartService cartService, INotificationService notificationService)
        {
            _context = context;
            _cartService = cartService;
            _notificationService = notificationService;
        }

        public async Task<OrderDto> CreateFromCartAsync(int buyerId)
        {
            var cart = await _context.Carts
                .Include(x => x.Items)
                .ThenInclude(x => x.Advertisement)
                .ThenInclude(x => x.Seller)
                .Include(x => x.Items)
                .ThenInclude(x => x.Advertisement)
                .ThenInclude(x => x.AdvertisementStatus)
                .FirstOrDefaultAsync(x => x.UserId == buyerId);

            if (cart is null || !cart.Items.Any())
            {
                throw new DomainValidationException("Cart is empty");
            }

            var invalidItem = cart.Items.FirstOrDefault(x => !IsPurchasable(x.Advertisement, buyerId));
            if (invalidItem is not null)
            {
                throw new DomainValidationException("Cart contains unavailable advertisements");
            }

            Order? lastOrder = null;
            var pendingStatusId = await EnsureOrderStatusAsync("pending");

            foreach (var item in cart.Items.ToList())
            {
                lastOrder = new Order
                {
                    AdvertisementId = item.AdvertisementId,
                    BuyerId = buyerId,
                    SellerId = item.Advertisement.SellerId,
                    OrderStatusId = pendingStatusId,
                    Price = item.Advertisement.Price,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(lastOrder);
                await _notificationService.CreateAsync(
                    item.Advertisement.SellerId,
                    "new_order",
                    "New order",
                    $"New order for '{item.Advertisement.Title}'");
            }

            await _context.SaveChangesAsync();
            await _cartService.ClearAsync(buyerId);
            return await GetByIdAsync(buyerId, lastOrder!.Id);
        }

        public async Task<OrderDto> CreateSingleAsync(int buyerId, int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Seller)
                .Include(x => x.AdvertisementStatus)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            if (!IsPurchasable(advertisement, buyerId))
            {
                throw new DomainValidationException("Advertisement is not available for ordering");
            }

            var order = new Order
            {
                AdvertisementId = advertisementId,
                BuyerId = buyerId,
                SellerId = advertisement.SellerId,
                OrderStatusId = await EnsureOrderStatusAsync("pending"),
                Price = advertisement.Price,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                advertisement.SellerId,
                "new_order",
                "New order",
                $"New order for '{advertisement.Title}'");

            return await GetByIdAsync(buyerId, order.Id);
        }

        public async Task<List<OrderDto>> GetBuyerOrdersAsync(int buyerId)
        {
            return await BuildOrdersQuery()
                .Where(x => x.BuyerId == buyerId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(MapOrder())
                .ToListAsync();
        }

        public async Task<List<OrderDto>> GetSellerOrdersAsync(int sellerId)
        {
            return await BuildOrdersQuery()
                .Where(x => x.SellerId == sellerId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(MapOrder())
                .ToListAsync();
        }

        public async Task CompleteAsync(int userId, int orderId)
        {
            var order = await GetOrderEntityForParticipantAsync(userId, orderId);
            var completedStatusId = await EnsureOrderStatusAsync("completed");

            if (order.OrderStatus.Name == "completed")
            {
                throw new DomainValidationException("Order is already completed");
            }

            if (order.OrderStatus.Name == "cancelled")
            {
                throw new DomainValidationException("Cancelled order cannot be completed");
            }

            order.OrderStatusId = completedStatusId;
            order.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(int userId, int orderId)
        {
            var order = await GetOrderEntityForParticipantAsync(userId, orderId);
            var cancelledStatusId = await EnsureOrderStatusAsync("cancelled");

            if (order.OrderStatus.Name == "completed")
            {
                throw new DomainValidationException("Completed order cannot be cancelled");
            }

            if (order.OrderStatus.Name == "cancelled")
            {
                throw new DomainValidationException("Order is already cancelled");
            }

            order.OrderStatusId = cancelledStatusId;
            await _context.SaveChangesAsync();
        }

        public async Task<OrderDto> GetByIdAsync(int userId, int orderId)
        {
            var order = await BuildOrdersQuery()
                .Where(x => x.Id == orderId && (x.BuyerId == userId || x.SellerId == userId))
                .Select(MapOrder())
                .FirstOrDefaultAsync();

            if (order is null)
            {
                throw new KeyNotFoundException("Order not found");
            }

            return order;
        }

        private IQueryable<Order> BuildOrdersQuery()
        {
            return _context.Orders
                .AsNoTracking()
                .Include(x => x.Advertisement)
                .Include(x => x.Buyer)
                .Include(x => x.Seller)
                .Include(x => x.OrderStatus);
        }

        private static System.Linq.Expressions.Expression<Func<Order, OrderDto>> MapOrder()
        {
            return x => new OrderDto
            {
                Id = x.Id,
                AdvertisementId = x.AdvertisementId,
                AdvertisementTitle = x.Advertisement.Title,
                BuyerId = x.BuyerId,
                BuyerName = x.Buyer.FullName,
                SellerName = x.Seller.FullName,
                Price = x.Price,
                Status = x.OrderStatus.Name,
                CreatedAt = x.CreatedAt,
                CompletedAt = x.CompletedAt
            };
        }

        private async Task<Order> GetOrderEntityForParticipantAsync(int userId, int orderId)
        {
            var order = await _context.Orders
                .Include(x => x.OrderStatus)
                .FirstOrDefaultAsync(x => x.Id == orderId && (x.BuyerId == userId || x.SellerId == userId));

            if (order is null)
            {
                throw new KeyNotFoundException("Order not found");
            }

            return order;
        }

        private static bool IsPurchasable(Advertisement advertisement, int buyerId)
        {
            return !advertisement.IsDeleted
                && !advertisement.IsArchived
                && advertisement.SellerId != buyerId
                && advertisement.AdvertisementStatus.Name == "approved";
        }

        private async Task<int> EnsureOrderStatusAsync(string name)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            var status = await _context.OrderStatuses.FirstOrDefaultAsync(x => x.Name == normalizedName);
            if (status is not null) return status.Id;

            status = new OrderStatus { Name = normalizedName };
            _context.OrderStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }
    }
}
