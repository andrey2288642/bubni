using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Encodings.Web;

namespace Bubnilka.Shared.Models;

/// <summary>
/// Модель сообщения с защитой от XSS
/// </summary>
public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChatId { get; set; }

    [ForeignKey(nameof(ChatId))]
    public Chat? Chat { get; set; }

    [Required]
    public int SenderId { get; set; }

    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    [Required]
    [MaxLength(4096, ErrorMessage = "Сообщение не может быть длиннее 4096 символов")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Экранированный контент для безопасного отображения (защита от XSS)
    /// </summary>
    [NotMapped]
    public string SafeContent => HtmlEncoder.Default.Encode(Content);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
}
