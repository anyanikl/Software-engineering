using FunApi.Interfaces;
using FunApi.Models;
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
                .FirstOrDefaultAsync(x => x.UserId == buyerId);

            if (cart is null || !cart.Items.Any())
            {
                throw new InvalidOperationException("Cart is empty");
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
            return await GetByIdAsync(lastOrder!.Id);
        }

        public async Task<OrderDto> CreateSingleAsync(int buyerId, int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
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

            return await GetByIdAsync(order.Id);
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
            var order = await GetOrderEntityAsync(orderId);
            if (order.BuyerId != userId && order.SellerId != userId)
            {
                throw new InvalidOperationException("Forbidden");
            }

            order.OrderStatusId = await EnsureOrderStatusAsync("completed");
            order.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(int userId, int orderId)
        {
            var order = await GetOrderEntityAsync(orderId);
            if (order.BuyerId != userId && order.SellerId != userId)
            {
                throw new InvalidOperationException("Forbidden");
            }

            order.OrderStatusId = await EnsureOrderStatusAsync("cancelled");
            await _context.SaveChangesAsync();
        }

        public async Task<OrderDto> GetByIdAsync(int orderId)
        {
            var order = await BuildOrdersQuery()
                .Where(x => x.Id == orderId)
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
                SellerId = x.SellerId,
                SellerName = x.Seller.FullName,
                Price = x.Price,
                Status = x.OrderStatus.Name,
                CreatedAt = x.CreatedAt,
                CompletedAt = x.CompletedAt
            };
        }

        private async Task<Order> GetOrderEntityAsync(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order is null)
            {
                throw new KeyNotFoundException("Order not found");
            }

            return order;
        }

        private async Task<int> EnsureOrderStatusAsync(string name)
        {
            var status = await _context.OrderStatuses.FirstOrDefaultAsync(x => x.Name == name);
            if (status is not null) return status.Id;

            status = new OrderStatus { Name = name };
            _context.OrderStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }
    }
}
