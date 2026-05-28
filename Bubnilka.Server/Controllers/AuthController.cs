using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Bubnilka.Server.Data;
using Bubnilka.Server.Services;
using Bubnilka.Shared.DTOs;
using Bubnilka.Shared.Models;
using System.Security.Claims;

namespace Bubnilka.Server.Controllers;

/// <summary>
/// Контроллер авторизации с BCrypt хешированием и безопасными токенами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    // Лимит попыток входа для защиты от брутфорса
    private static readonly Dictionary<string, (int count, DateTime lockUntil)> _loginAttempts = new();
    private const int MaxLoginAttempts = 5;
    private const int LockoutMinutes = 15;

    public AuthController(
        AppDbContext context,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(AuthResponse.ErrorResponse("Некорректные данные"));
        }

        // Проверяем существует ли пользователь
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict(AuthResponse.ErrorResponse("Пользователь с таким email уже существует"));
        }

        // Хешируем пароль с BCrypt (защита от утечки паролей)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName ?? request.Email.Split('@')[0],
            IsOnline = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {Email} зарегистрирован", user.Email);

        var token = _tokenService.GenerateToken(user);

        return Ok(AuthResponse.SuccessResponse(token, user.Id, user.Email, user.DisplayName));
    }

    /// <summary>
    /// Вход пользователя с защитой от брутфорса
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(AuthResponse.ErrorResponse("Некорректные данные"));
        }

        // Проверка на lockout после множественных неудачных попыток
        if (_loginAttempts.TryGetValue(request.Email, out var attemptInfo))
        {
            if (attemptInfo.lockUntil > DateTime.UtcNow)
            {
                var remainingMinutes = (int)(attemptInfo.lockUntil - DateTime.UtcNow).TotalMinutes + 1;
                return Unauthorized(AuthResponse.ErrorResponse($"Слишком много попыток входа. Попробуйте через {remainingMinutes} мин."));
            }
            else
            {
                // Сбрасываем счетчик если lockout истек
                _loginAttempts.Remove(request.Email);
            }
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            // Записываем попытку для защиты от перебора email
            RecordFailedAttempt(request.Email);
            return Unauthorized(AuthResponse.ErrorResponse("Неверный email или пароль"));
        }

        // Проверяем пароль с BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            RecordFailedAttempt(request.Email);
            _logger.LogWarning("Неверный пароль для пользователя {Email}", user.Email);
            return Unauthorized(AuthResponse.ErrorResponse("Неверный email или пароль"));
        }

        // Успешный вход - сбрасываем счетчик попыток
        _loginAttempts.Remove(request.Email);

        // Обновляем статус онлайн
        user.IsOnline = true;
        user.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {Email} вошел в систему", user.Email);

        var token = _tokenService.GenerateToken(user);

        return Ok(AuthResponse.SuccessResponse(token, user.Id, user.Email, user.DisplayName));
    }

    /// <summary>
    /// Выход пользователя
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsOnline = false;
            user.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Пользователь {Email} вышел из системы", user.Email);
        }

        return Ok(new { Success = true });
    }

    /// <summary>
    /// Получение текущего пользователя
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsOnline,
            user.LastSeenAt
        });
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

    private void RecordFailedAttempt(string email)
    {
        if (!_loginAttempts.TryGetValue(email, out var attemptInfo))
        {
            attemptInfo = (0, DateTime.UtcNow);
        }

        attemptInfo.count++;
        if (attemptInfo.count >= MaxLoginAttempts)
        {
            attemptInfo.lockUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            _logger.LogWarning("Аккаунт {Email} заблокирован на {Minutes} мин из-за множественных неудачных попыток", email, LockoutMinutes);
        }

        _loginAttempts[email] = attemptInfo;
    }
}
