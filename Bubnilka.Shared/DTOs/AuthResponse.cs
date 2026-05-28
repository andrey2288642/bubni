namespace Bubnilka.Shared.DTOs;

/// <summary>
/// DTO для ответа авторизации без чувствительных данных
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }

    public string? Token { get; set; }

    public int UserId { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Создает успешный ответ
    /// </summary>
    public static AuthResponse SuccessResponse(string token, int userId, string email, string? displayName)
    {
        return new AuthResponse
        {
            Success = true,
            Token = token,
            UserId = userId,
            Email = email,
            DisplayName = displayName
        };
    }

    /// <summary>
    /// Создает ответ с ошибкой
    /// </summary>
    public static AuthResponse ErrorResponse(string message)
    {
        return new AuthResponse
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
