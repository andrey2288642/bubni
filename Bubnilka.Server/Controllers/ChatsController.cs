using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bubnilka.Server.Data;
using Bubnilka.Shared.DTOs;
using Bubnilka.Shared.Models;
using System.Security.Claims;

namespace Bubnilka.Server.Controllers;

/// <summary>
/// Контроллер чатов с авторизацией и валидацией
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatsController> _logger;

    public ChatsController(
        AppDbContext context,
        ILogger<ChatsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получение списка чатов текущего пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetChats()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentUserId = userId.Value;

        var chats = await _context.Chats
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages)
            .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var result = chats.Select(c => new
        {
            c.Id,
            OtherUserId = c.User1Id == currentUserId ? c.User2Id : c.User1Id,
            OtherUserEmail = c.User1Id == currentUserId ? c.User2!.Email : c.User1!.Email,
            OtherUserDisplayName = c.User1Id == currentUserId ? c.User2!.DisplayName : c.User1!.DisplayName,
            OtherUserIsOnline = c.User1Id == currentUserId ? c.User2!.IsOnline : c.User1!.IsOnline,
            LastMessageAt = c.LastMessageAt,
            MessagesCount = c.Messages?.Count ?? 0
        });

        return Ok(result);
    }

    /// <summary>
    /// Создание или получение существующего чата
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreateChat([FromBody] CreateChatRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentUserId = userId.Value;

        // Валидация входных данных
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Error = "Некорректные данные" });
        }

        // Проверка что пользователь не создает чат сам с собой
        if (request.OtherUserId == currentUserId)
        {
            return BadRequest(new { Error = "Нельзя создать чат сам с собой" });
        }

        // Проверка что собеседник существует
        var otherUser = await _context.Users.FindAsync(request.OtherUserId);
        if (otherUser == null)
        {
            return NotFound(new { Error = "Пользователь не найден" });
        }

        // Проверяем существует ли уже чат между этими пользователями
        var existingChat = await _context.Chats
            .FirstOrDefaultAsync(c => 
                (c.User1Id == currentUserId && c.User2Id == request.OtherUserId) ||
                (c.User1Id == request.OtherUserId && c.User2Id == currentUserId));

        if (existingChat != null)
        {
            return Ok(new
            {
                existingChat.Id,
                Message = "Чат уже существует"
            });
        }

        // Создаем новый чат
        var chat = new Chat
        {
            User1Id = currentUserId,
            User2Id = request.OtherUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Создан чат между пользователями {UserId} и {OtherUserId}", currentUserId, request.OtherUserId);

        return Ok(new
        {
            chat.Id,
            Message = "Чат создан"
        });
    }

    /// <summary>
    /// Получение сообщений чата
    /// </summary>
    [HttpGet("{chatId}/messages")]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(int chatId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentUserId = userId.Value;

        // Проверяем что пользователь является участником чата
        var chat = await _context.Chats
            .Include(c => c.Messages)
            .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId && (c.User1Id == currentUserId || c.User2Id == currentUserId));

        if (chat == null)
        {
            return NotFound(new { Error = "Чат не найден или доступ запрещен" });
        }

        var messages = chat.Messages?
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Content,
                SenderId = m.SenderId,
                SenderName = m.Sender?.DisplayName ?? m.Sender?.Email ?? "Unknown",
                m.CreatedAt,
                m.IsRead
            }) ?? Enumerable.Empty<object>();

        return Ok(messages);
    }

    /// <summary>
    /// Отправка сообщения (дублирование через API для надежности)
    /// </summary>
    [HttpPost("{chatId}/messages")]
    public async Task<ActionResult<object>> SendMessage(int chatId, [FromBody] object request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentUserId = userId.Value;

        // Проверяем что пользователь является участником чата
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null || (chat.User1Id != currentUserId && chat.User2Id != currentUserId))
        {
            return NotFound(new { Error = "Чат не найден или доступ запрещен" });
        }

        // Парсим контент сообщения из dynamic объекта
        var contentProp = request.GetType().GetProperty("content");
        if (contentProp == null)
        {
            return BadRequest(new { Error = "Отсутствует поле content" });
        }
        
        var content = contentProp.GetValue(request)?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content) || content.Length > 4096)
        {
            return BadRequest(new { Error = "Сообщение должно быть от 1 до 4096 символов" });
        }

        var message = new Message
        {
            ChatId = chatId,
            SenderId = currentUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        
        // Обновляем время последнего сообщения
        chat.LastMessageAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {UserId} отправил сообщение в чат {ChatId}", currentUserId, chatId);

        return Ok(new
        {
            message.Id,
            message.Content,
            message.SenderId,
            message.CreatedAt
        });
    }

    /// <summary>
    /// Поиск пользователей по email
    /// </summary>
    [HttpGet("users/search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchUsers([FromQuery] string query)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest(new { Error = "Запрос должен содержать минимум 2 символа" });
        }

        var users = await _context.Users
            .Where(u => u.Id != userId && (u.Email.Contains(query) || (u.DisplayName != null && u.DisplayName.Contains(query))))
            .Take(10)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.IsOnline,
                u.LastSeenAt
            })
            .ToListAsync();

        return Ok(users);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
