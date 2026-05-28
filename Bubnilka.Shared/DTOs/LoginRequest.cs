using System.ComponentModel.DataAnnotations;

namespace Bubnilka.Shared.DTOs;

/// <summary>
/// DTO для входа с безопасной передачей данных
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(256, ErrorMessage = "Email не может быть длиннее 256 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
    [MaxLength(128, ErrorMessage = "Пароль не может быть длиннее 128 символов")]
    public string Password { get; set; } = string.Empty;
}
