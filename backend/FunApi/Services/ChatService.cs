using FunApi.Exceptions;
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
                    AdvertisementId = x.AdvertisementId,
                    InterlocutorId = x.BuyerId == userId ? x.SellerId : x.BuyerId,
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
                InterlocutorId = chat.BuyerId == userId ? chat.SellerId : chat.BuyerId,
                InterlocutorName = chat.BuyerId == userId ? chat.Seller.FullName : chat.Buyer.FullName,
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

        public async Task<ChatDto> GetOrCreateAsync(int currentUserId, int advertisementId, int? participantUserId = null)
        {
            var advertisement = await _context.Advertisements
                .Include(x => x.Seller)
                .Include(x => x.AdvertisementStatus)
                .FirstOrDefaultAsync(x => x.Id == advertisementId && !x.IsDeleted);

            if (advertisement is null)
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            if (currentUserId == advertisement.SellerId)
            {
                return await GetOrCreateSellerSideChatAsync(currentUserId, advertisementId, participantUserId);
            }

            if (advertisement.IsArchived || advertisement.AdvertisementStatus.Name != "approved")
            {
                throw new KeyNotFoundException("Advertisement not found");
            }

            if (participantUserId.HasValue && participantUserId.Value != advertisement.SellerId)
            {
                throw new DomainValidationException("Participant does not match advertisement seller");
            }

            var chat = await _context.Chats
                .FirstOrDefaultAsync(x =>
                    x.AdvertisementId == advertisementId
                    && x.BuyerId == currentUserId
                    && x.SellerId == advertisement.SellerId);

            if (chat is null)
            {
                chat = new Chat
                {
                    AdvertisementId = advertisementId,
                    BuyerId = currentUserId,
                    SellerId = advertisement.SellerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();
            }

            return await GetByIdAsync(currentUserId, chat.Id);
        }

        public async Task<MessageDto> SendMessageAsync(int senderId, SendMessageDto dto)
        {
            var content = dto.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new DomainValidationException("Message content is required");
            }

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
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            chat.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var recipientId = chat.BuyerId == senderId ? chat.SellerId : chat.BuyerId;
            await _notificationService.CreateAsync(recipientId, "new_message", "New message", content);

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
            var chatExists = await _context.Chats
                .AnyAsync(x => x.Id == chatId && (x.BuyerId == userId || x.SellerId == userId));

            if (!chatExists)
            {
                throw new KeyNotFoundException("Chat not found");
            }

            var messages = await _context.Messages
                .Where(x => x.ChatId == chatId && x.SenderId != userId && !x.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ChatDto> GetOrCreateSellerSideChatAsync(int sellerId, int advertisementId, int? participantUserId)
        {
            if (!participantUserId.HasValue)
            {
                throw new DomainValidationException("Participant user is required for seller-side chat creation");
            }

            if (participantUserId.Value == sellerId)
            {
                throw new DomainValidationException("Cannot create a chat with yourself");
            }

            var existingChat = await _context.Chats
                .FirstOrDefaultAsync(x =>
                    x.AdvertisementId == advertisementId
                    && x.SellerId == sellerId
                    && x.BuyerId == participantUserId.Value);

            if (existingChat is not null)
            {
                return await GetByIdAsync(sellerId, existingChat.Id);
            }

            var participantExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == participantUserId.Value && !x.IsBlocked);

            if (!participantExists)
            {
                throw new KeyNotFoundException("User not found");
            }

            var hasOrderForAdvertisement = await _context.Orders
                .AsNoTracking()
                .AnyAsync(x =>
                    x.AdvertisementId == advertisementId
                    && x.SellerId == sellerId
                    && x.BuyerId == participantUserId.Value);

            if (!hasOrderForAdvertisement)
            {
                throw new ForbiddenException("Seller can start a chat only with a buyer who has an order for this advertisement");
            }

            var chat = new Chat
            {
                AdvertisementId = advertisementId,
                BuyerId = participantUserId.Value,
                SellerId = sellerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(sellerId, chat.Id);
        }
    }
}
