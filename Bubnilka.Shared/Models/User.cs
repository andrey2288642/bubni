using System.ComponentModel.DataAnnotations;

namespace Bubnilka.Shared.Models;

/// <summary>
/// Модель пользователя с валидацией данных
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(256, ErrorMessage = "Email не может быть длиннее 256 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
    [MaxLength(128, ErrorMessage = "Пароль не может быть длиннее 128 символов")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
    public string? DisplayName { get; set; }

    public bool IsOnline { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSeenAt { get; set; }
}
