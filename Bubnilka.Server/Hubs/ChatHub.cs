using Microsoft.AspNetCore.SignalR;
using Bubnilka.Server.Data;
using Bubnilka.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bubnilka.Server.Hubs;

/// <summary>
/// SignalR хаб для чата с безопасной обработкой
/// </summary>
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext context, ILogger<ChatHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Отправка сообщения через SignalR
    /// </summary>
    public async Task SendMessage(int chatId, string content)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            await Clients.Caller.SendAsync("Error", "Неавторизованный доступ");
            return;
        }

        // Валидация контента
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4096)
        {
            await Clients.Caller.SendAsync("Error", "Некорректное сообщение");
            return;
        }

        // Проверяем что пользователь является участником чата
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
        {
            await Clients.Caller.SendAsync("Error", "Чат не найден или доступ запрещен");
            return;
        }

        // Создаем сообщение
        var message = new Message
        {
            ChatId = chatId,
            SenderId = userId.Value,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        
        // Обновляем время последнего сообщения
        chat.LastMessageAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // Получаем данные отправителя
        var sender = await _context.Users.FindAsync(userId.Value);
        
        // Отправляем сообщение всем участникам чата
        await Clients.Group(GetChatGroupName(chatId)).SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.Content,
            SenderId = message.SenderId,
            SenderName = sender?.DisplayName ?? sender?.Email ?? "Unknown",
            message.CreatedAt,
            ChatId = chatId
        });

        _logger.LogInformation("Сообщение отправлено в чат {ChatId} пользователем {UserId}", chatId, userId);
    }

    /// <summary>
    /// Присоединение к группе чата
    /// </summary>
    public async Task JoinChat(int chatId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            await Clients.Caller.SendAsync("Error", "Неавторизованный доступ");
            return;
        }

        // Проверяем что пользователь является участником чата
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
        {
            await Clients.Caller.SendAsync("Error", "Чат не найден или доступ запрещен");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetChatGroupName(chatId));
        
        _logger.LogInformation("Пользователь {UserId} присоединился к чату {ChatId}", userId, chatId);
    }

    /// <summary>
    /// Выход из группы чата
    /// </summary>
    public async Task LeaveChat(int chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetChatGroupName(chatId));
        
        _logger.LogInformation("Пользователь покинул чат {ChatId}", chatId);
    }

    /// <summary>
    /// Обновление статуса онлайн
    /// </summary>
    public async Task UpdateStatus(bool isOnline)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return;
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user != null)
        {
            user.IsOnline = isOnline;
            if (isOnline)
            {
                user.LastSeenAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            // Уведомляем все чаты пользователя об изменении статуса
            var chats = await _context.Chats
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .ToListAsync();

            foreach (var chat in chats)
            {
                await Clients.Group(GetChatGroupName(chat.Id)).SendAsync("UserStatusChanged", new
                {
                    UserId = userId.Value,
                    IsOnline = isOnline,
                    LastSeenAt = user.LastSeenAt
                });
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Обновляем статус на офлайн при отключении
        await UpdateStatus(false);
        
        await base.OnDisconnectedAsync(exception);
        
        _logger.LogInformation("Клиент отключился: {ConnectionId}", Context.ConnectionId);
    }

    private int? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }

    private static string GetChatGroupName(int chatId) => $"chat_{chatId}";
}
