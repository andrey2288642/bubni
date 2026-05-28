using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bubnilka.Shared.Models;

/// <summary>
/// Модель чата с правильными связями
/// </summary>
public class Chat
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int User1Id { get; set; }

    [Required]
    public int User2Id { get; set; }

    [ForeignKey(nameof(User1Id))]
    public User? User1 { get; set; }

    [ForeignKey(nameof(User2Id))]
    public User? User2 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }

    // Навигационное свойство для сообщений
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
