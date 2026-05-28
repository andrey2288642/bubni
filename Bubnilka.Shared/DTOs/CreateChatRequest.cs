using System.ComponentModel.DataAnnotations;

namespace Bubnilka.Shared.DTOs;

/// <summary>
/// DTO для создания чата с проверкой входных данных
/// </summary>
public class CreateChatRequest
{
    [Required(ErrorMessage = "ID собеседника обязателен")]
    [Range(1, int.MaxValue, ErrorMessage = "Некорректный ID собеседника")]
    public int OtherUserId { get; set; }
}
