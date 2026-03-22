using FunApi.Interfaces;
using FunApi.Models;
using FunApi.Models.Chats;
using FunDto.Models.Contracts.Chats;
using Microsoft.EntityFrameworkCore;

namespace FunApi.Services
{
    public class ChatService : IChatService
    {
        private readonly FunDBcontext _context;
        private readonly INotificationService _notificationService;

        public ChatService(FunDBcontext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<ChatListItemDto>> GetMyChatsAsync(int userId)
        {
            return await _context.Chats
                .AsNoTracking()
                .Where(x => x.BuyerId == userId || x.SellerId == userId)
                .Include(x => x.Buyer)
                .Include(x => x.Seller)
                .Include(x => x.Advertisement)
                .Include(x => x.Messages)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new ChatListItemDto
                {
                    Id = x.Id,
                    InterlocutorName = x.BuyerId == userId ? x.Seller.FullName : x.Buyer.FullName,
                    AdvertisementTitle = x.Advertisement.Title,
                    LastMessage = x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                    LastMessageAt = x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault(),
                    UnreadCount = x.Messages.Count(m => !m.IsRead && m.SenderId != userId)
                })
                .ToListAsync();
        }

        public async Task<ChatDto> GetByIdAsync(int userId, int chatId)
        {
            var chat = await _context.Chats
                .Include(x => x.Advertisement)
                .Include(x => x.Buyer)
                .Include(x => x.Seller)
                .Include(x => x.Messages)
                .ThenInclude(x => x.Sender)
                .FirstOrDefaultAsync(x => x.Id == chatId && (x.BuyerId == userId || x.SellerId == userId));

            if (chat is null)
            {
                throw new KeyNotFoundException("Chat not found");
            }

            return new ChatDto
            {
                Id = chat.Id,
                AdvertisementId = chat.AdvertisementId,
                AdvertisementTitle = chat.Advertisement.Title,
                BuyerId = chat.BuyerId,
                BuyerName = chat.Buyer.FullName,
                SellerId = chat.SellerId,
                SellerName = chat.Seller.FullName,
                Messages = chat.Messages
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => new MessageDto
                    {
                        Id = x.Id,
                        SenderId = x.SenderId,
                        SenderName = x.Sender.FullName,
                        Content = x.Content,
                        IsRead = x.IsRead,
                        CreatedAt = x.CreatedAt
                    })
                    .ToList()
            };
        }

        public async Task<ChatDto> GetOrCreateAsync(int buyerId, int advertisementId)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            var chat = await _context.Chats.FirstOrDefaultAsync(x => x.AdvertisementId == advertisementId && x.BuyerId == buyerId);
            if (chat is null)
            {
                chat = new Chat
                {
                    AdvertisementId = advertisementId,
                    BuyerId = buyerId,
                    SellerId = advertisement.SellerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();
            }

            return await GetByIdAsync(buyerId, chat.Id);
        }

        public async Task<MessageDto> SendMessageAsync(int senderId, SendMessageDto dto)
        {
            var chat = await _context.Chats
                .Include(x => x.Buyer)
                .Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == dto.ChatId && (x.BuyerId == senderId || x.SellerId == senderId));

            if (chat is null)
            {
                throw new KeyNotFoundException("Chat not found");
            }

            var message = new Message
            {
                ChatId = dto.ChatId,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            chat.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var recipientId = chat.BuyerId == senderId ? chat.SellerId : chat.BuyerId;
            await _notificationService.CreateAsync(recipientId, "new_message", "New message", dto.Content.Trim());

            return await _context.Messages
                .AsNoTracking()
                .Where(x => x.Id == message.Id)
                .Include(x => x.Sender)
                .Select(x => new MessageDto
                {
                    Id = x.Id,
                    SenderId = x.SenderId,
                    SenderName = x.Sender.FullName,
                    Content = x.Content,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                })
                .FirstAsync();
        }

        public async Task MarkAsReadAsync(int userId, int chatId)
        {
            var messages = await _context.Messages
                .Where(x => x.ChatId == chatId && x.SenderId != userId && !x.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
