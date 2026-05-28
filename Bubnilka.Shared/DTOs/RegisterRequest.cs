using System.ComponentModel.DataAnnotations;

namespace Bubnilka.Shared.DTOs;

/// <summary>
/// DTO для регистрации с валидацией email
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(256, ErrorMessage = "Email не может быть длиннее 256 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
    [MaxLength(128, ErrorMessage = "Пароль не может быть длиннее 128 символов")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Пароль должен содержать заглавные буквы, строчные буквы и цифры")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
    public string? DisplayName { get; set; }
}
